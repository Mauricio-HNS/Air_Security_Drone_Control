using System.Collections.Concurrent;
using AirSecurityDroneControl.BuildingBlocks.Contracts;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<CommandCenterState>();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    service = "CommandCenter.Api",
    status = "running",
    utc = DateTimeOffset.UtcNow
}));

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/api/projections/tracks", (FusedTrack track, CommandCenterState state) =>
{
    state.Tracks[track.TrackId] = track;
    return Results.Accepted($"/api/projections/tracks/{track.TrackId}", track);
});

app.MapPost("/api/projections/threats", (ThreatAssessment threat, CommandCenterState state) =>
{
    state.Threats[threat.AssessmentId] = threat;
    return Results.Accepted($"/api/projections/threats/{threat.AssessmentId}", threat);
});

app.MapPost("/api/projections/incidents", (IncidentCase incident, CommandCenterState state) =>
{
    state.Incidents[incident.IncidentId] = incident;
    return Results.Accepted($"/api/projections/incidents/{incident.IncidentId}", incident);
});

app.MapPost("/api/projections/sensors", (SensorNodeStatus sensor, CommandCenterState state) =>
{
    state.Sensors[sensor.SensorNodeId] = sensor;
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

public sealed class CommandCenterState
{
    public ConcurrentDictionary<Guid, FusedTrack> Tracks { get; } = new();
    public ConcurrentDictionary<Guid, ThreatAssessment> Threats { get; } = new();
    public ConcurrentDictionary<Guid, IncidentCase> Incidents { get; } = new();
    public ConcurrentDictionary<string, SensorNodeStatus> Sensors { get; } = new();
}
