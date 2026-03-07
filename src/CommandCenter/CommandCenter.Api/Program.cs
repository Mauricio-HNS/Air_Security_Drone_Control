using System.Collections.Concurrent;
using AirSecurityDroneControl.BuildingBlocks.Contracts;
using AirSecurityDroneControl.BuildingBlocks.Infrastructure;
using AirSecurityDroneControl.BuildingBlocks.Observability;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<CommandCenterState>();
builder.Services.AddSingleton<BasicMetrics>();
var dataDir = Path.Combine(Directory.GetCurrentDirectory(), ".runtime", "data", "command-center");
builder.Services.AddSingleton(new JsonFileStore<FusedTrack>(dataDir, "tracks.json"));
builder.Services.AddSingleton(new JsonFileStore<ThreatAssessment>(dataDir, "threats.json"));
builder.Services.AddSingleton(new JsonFileStore<IncidentCase>(dataDir, "incidents.json"));
builder.Services.AddSingleton(new JsonFileStore<SensorNodeStatus>(dataDir, "sensors.json"));
builder.Services.AddSingleton(new EventLog(dataDir));

var app = builder.Build();
app.UseMiddleware<BasicMetricsMiddleware>();
app.Use(async (context, next) =>
{
    if (context.Request.Method == HttpMethods.Get)
    {
        await next();
        return;
    }

    if (!HasAccess(context, app.Configuration))
    {
        return;
    }

    await next();
});
SeedData(app.Services);

app.MapGet("/", () => Results.Ok(new
{
    service = "CommandCenter.Api",
    status = "running",
    utc = DateTimeOffset.UtcNow
}));

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/metrics/basic", (BasicMetrics metrics) => Results.Ok(metrics.Snapshot()));

app.MapPost("/api/projections/tracks", async (
    FusedTrack track,
    CommandCenterState state,
    JsonFileStore<FusedTrack> store,
    EventLog eventLog,
    CancellationToken ct) =>
{
    state.Tracks[track.TrackId] = track;
    await store.WriteAllAsync(state.Tracks.Values.OrderByDescending(x => x.LastUpdateUtc), ct);
    await eventLog.AppendAsync(new DomainEvent(Guid.NewGuid(), "TrackProjected", DateTimeOffset.UtcNow, track), ct);
    return Results.Accepted($"/api/projections/tracks/{track.TrackId}", track);
});

app.MapPost("/api/projections/threats", async (
    ThreatAssessment threat,
    CommandCenterState state,
    JsonFileStore<ThreatAssessment> store,
    EventLog eventLog,
    CancellationToken ct) =>
{
    state.Threats[threat.AssessmentId] = threat;
    await store.WriteAllAsync(state.Threats.Values.OrderByDescending(x => x.TimestampUtc), ct);
    await eventLog.AppendAsync(new DomainEvent(Guid.NewGuid(), "ThreatProjected", DateTimeOffset.UtcNow, threat), ct);
    return Results.Accepted($"/api/projections/threats/{threat.AssessmentId}", threat);
});

app.MapPost("/api/projections/incidents", async (
    IncidentCase incident,
    CommandCenterState state,
    JsonFileStore<IncidentCase> store,
    EventLog eventLog,
    CancellationToken ct) =>
{
    state.Incidents[incident.IncidentId] = incident;
    await store.WriteAllAsync(state.Incidents.Values.OrderByDescending(x => x.CreatedAtUtc), ct);
    await eventLog.AppendAsync(new DomainEvent(Guid.NewGuid(), "IncidentProjected", DateTimeOffset.UtcNow, incident), ct);
    return Results.Accepted($"/api/projections/incidents/{incident.IncidentId}", incident);
});

app.MapPost("/api/projections/sensors", async (
    SensorNodeStatus sensor,
    CommandCenterState state,
    JsonFileStore<SensorNodeStatus> store,
    EventLog eventLog,
    CancellationToken ct) =>
{
    state.Sensors[sensor.SensorNodeId] = sensor;
    await store.WriteAllAsync(state.Sensors.Values.OrderByDescending(x => x.LastSeenUtc), ct);
    await eventLog.AppendAsync(new DomainEvent(Guid.NewGuid(), "SensorProjected", DateTimeOffset.UtcNow, sensor), ct);
    return Results.Accepted($"/api/projections/sensors/{sensor.SensorNodeId}", sensor);
});

app.MapGet("/api/tracks", (CommandCenterState state, int limit = 100) =>
{
    var tracks = state.Tracks.Values
        .OrderByDescending(x => x.LastUpdateUtc)
        .Take(Math.Clamp(limit, 1, 1000))
        .ToArray();

    return Results.Ok(tracks);
});

app.MapGet("/api/incidents", (CommandCenterState state, int limit = 100) =>
{
    var incidents = state.Incidents.Values
        .OrderByDescending(x => x.CreatedAtUtc)
        .Take(Math.Clamp(limit, 1, 1000))
        .ToArray();

    return Results.Ok(incidents);
});

app.MapGet("/api/sensors/status", (CommandCenterState state) =>
{
    var sensors = state.Sensors.Values
        .OrderByDescending(x => x.LastSeenUtc)
        .ToArray();

    return Results.Ok(sensors);
});

app.MapGet("/api/replay/{incidentId:guid}", (Guid incidentId, CommandCenterState state) =>
{
    if (!state.Incidents.TryGetValue(incidentId, out var incident))
    {
        return Results.NotFound();
    }

    state.Threats.TryGetValue(incident.ThreatAssessmentId, out var threat);
    state.Tracks.TryGetValue(incident.TrackId, out var track);

    var timeline = new List<string>
    {
        $"Incident opened at {incident.CreatedAtUtc:O} in zone {incident.Zone}.",
        $"Current status: {incident.Status}."
    };

    if (track is not null)
    {
        timeline.Add(
            $"Track {track.TrackId} confidence {track.Confidence:P0}, heading {track.EstimatedHeadingDegrees:F0} deg.");
    }

    if (threat is not null)
    {
        timeline.Add(
            $"Threat score {threat.Score:F1} ({threat.Level}) generated at {threat.TimestampUtc:O}.");
        timeline.Add(threat.Summary);
    }

    return Results.Ok(new
    {
        incident,
        threat,
        track,
        timeline
    });
});

app.MapGet("/api/overview", (CommandCenterState state) =>
{
    var activeIncidents = state.Incidents.Values
        .Where(x => x.Status is IncidentStatus.Open or IncidentStatus.UnderInvestigation)
        .OrderByDescending(x => x.CreatedAtUtc)
        .ToArray();

    var latestThreats = state.Threats.Values
        .OrderByDescending(x => x.TimestampUtc)
        .Take(20)
        .ToArray();

    var latestTracks = state.Tracks.Values
        .OrderByDescending(x => x.LastUpdateUtc)
        .Take(20)
        .ToArray();

    return Results.Ok(new
    {
        utc = DateTimeOffset.UtcNow,
        activeIncidentCount = activeIncidents.Length,
        sensorsOnline = state.Sensors.Values.Count(x => x.Health == SensorNodeHealth.Online),
        sensorCount = state.Sensors.Count,
        incidents = activeIncidents,
        threats = latestThreats,
        tracks = latestTracks
    });
});

app.Run();

static void SeedData(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var state = scope.ServiceProvider.GetRequiredService<CommandCenterState>();
    var trackStore = scope.ServiceProvider.GetRequiredService<JsonFileStore<FusedTrack>>();
    var threatStore = scope.ServiceProvider.GetRequiredService<JsonFileStore<ThreatAssessment>>();
    var incidentStore = scope.ServiceProvider.GetRequiredService<JsonFileStore<IncidentCase>>();
    var sensorStore = scope.ServiceProvider.GetRequiredService<JsonFileStore<SensorNodeStatus>>();

    foreach (var track in trackStore.ReadAllAsync().GetAwaiter().GetResult())
    {
        state.Tracks[track.TrackId] = track;
    }

    foreach (var threat in threatStore.ReadAllAsync().GetAwaiter().GetResult())
    {
        state.Threats[threat.AssessmentId] = threat;
    }

    foreach (var incident in incidentStore.ReadAllAsync().GetAwaiter().GetResult())
    {
        state.Incidents[incident.IncidentId] = incident;
    }

    foreach (var sensor in sensorStore.ReadAllAsync().GetAwaiter().GetResult())
    {
        state.Sensors[sensor.SensorNodeId] = sensor;
    }
}

static bool HasAccess(HttpContext context, IConfiguration configuration)
{
    var key = context.Request.Headers["X-API-Key"].FirstOrDefault();
    if (!string.Equals(key, configuration["Security:ApiKey"], StringComparison.Ordinal))
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return false;
    }

    var role = context.Request.Headers["X-Role"].FirstOrDefault();
    var ok = string.Equals(role, "operator", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase);
    if (!ok)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return false;
    }

    return true;
}

public sealed class CommandCenterState
{
    public ConcurrentDictionary<Guid, FusedTrack> Tracks { get; } = new();
    public ConcurrentDictionary<Guid, ThreatAssessment> Threats { get; } = new();
    public ConcurrentDictionary<Guid, IncidentCase> Incidents { get; } = new();
    public ConcurrentDictionary<string, SensorNodeStatus> Sensors { get; } = new();
}
