using System.Collections.Concurrent;
using AirSecurityDroneControl.BuildingBlocks.Contracts;
using AirSecurityDroneControl.BuildingBlocks.Infrastructure;
using AirSecurityDroneControl.BuildingBlocks.Observability;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ConcurrentDictionary<Guid, IncidentCase>>();
builder.Services.AddSingleton<BasicMetrics>();
var dataDir = Path.Combine(Directory.GetCurrentDirectory(), ".runtime", "data", "incidents");
builder.Services.AddSingleton(new JsonFileStore<IncidentCase>(dataDir, "incidents.json"));
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
    service = "Incidents.Api",
    status = "running",
    utc = DateTimeOffset.UtcNow
}));

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/metrics/basic", (BasicMetrics metrics) => Results.Ok(metrics.Snapshot()));

app.MapPost("/api/incidents/open", async (
    OpenIncidentRequest request,
    ConcurrentDictionary<Guid, IncidentCase> incidents,
    JsonFileStore<IncidentCase> incidentStore,
    EventLog eventLog,
    CancellationToken ct) =>
{
    var incident = new IncidentCase(
        IncidentId: Guid.NewGuid(),
        TrackId: request.TrackId,
        ThreatAssessmentId: request.ThreatAssessmentId,
        Status: IncidentStatus.Open,
        Zone: request.Zone,
        CreatedAtUtc: DateTimeOffset.UtcNow);

    incidents[incident.IncidentId] = incident;
    await incidentStore.WriteAllAsync(incidents.Values.OrderByDescending(x => x.CreatedAtUtc), ct);
    await eventLog.AppendAsync(
        new DomainEvent(Guid.NewGuid(), "IncidentOpened", DateTimeOffset.UtcNow, incident), ct);
    return Results.Created($"/api/incidents/{incident.IncidentId}", incident);
});

app.MapPatch("/api/incidents/{incidentId:guid}/status", async (
    Guid incidentId,
    UpdateIncidentStatusRequest request,
    ConcurrentDictionary<Guid, IncidentCase> incidents,
    JsonFileStore<IncidentCase> incidentStore,
    EventLog eventLog,
    CancellationToken ct) =>
{
    if (!incidents.TryGetValue(incidentId, out var current))
    {
        return Results.NotFound();
    }

    DateTimeOffset? closedAt = request.Status is IncidentStatus.Resolved or IncidentStatus.Dismissed
        ? DateTimeOffset.UtcNow
        : null;

    var updated = current with
    {
        Status = request.Status,
        ClosedAtUtc = closedAt
    };

    incidents[incidentId] = updated;
    await incidentStore.WriteAllAsync(incidents.Values.OrderByDescending(x => x.CreatedAtUtc), ct);
    await eventLog.AppendAsync(
        new DomainEvent(Guid.NewGuid(), "IncidentStatusUpdated", DateTimeOffset.UtcNow, updated), ct);
    return Results.Ok(updated);
});

app.MapGet("/api/incidents", (
    ConcurrentDictionary<Guid, IncidentCase> incidents,
    int limit = 100) =>
{
    var data = incidents.Values
        .OrderByDescending(x => x.CreatedAtUtc)
        .Take(Math.Clamp(limit, 1, 1000))
        .ToArray();

    return Results.Ok(data);
});

app.Run();

static void SeedData(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var incidents = scope.ServiceProvider.GetRequiredService<ConcurrentDictionary<Guid, IncidentCase>>();
    var store = scope.ServiceProvider.GetRequiredService<JsonFileStore<IncidentCase>>();

    foreach (var incident in store.ReadAllAsync().GetAwaiter().GetResult())
    {
        incidents[incident.IncidentId] = incident;
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

public record OpenIncidentRequest(Guid TrackId, Guid ThreatAssessmentId, string Zone);
public record UpdateIncidentStatusRequest(IncidentStatus Status);
