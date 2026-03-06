using System.Collections.Concurrent;
using AirSecurityDroneControl.BuildingBlocks.Contracts;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ConcurrentDictionary<Guid, IncidentCase>>();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    service = "Incidents.Api",
    status = "running",
    utc = DateTimeOffset.UtcNow
}));

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/api/incidents/open", (
    OpenIncidentRequest request,
    ConcurrentDictionary<Guid, IncidentCase> incidents) =>
{
    var incident = new IncidentCase(
        IncidentId: Guid.NewGuid(),
        TrackId: request.TrackId,
        ThreatAssessmentId: request.ThreatAssessmentId,
        Status: IncidentStatus.Open,
        Zone: request.Zone,
        CreatedAtUtc: DateTimeOffset.UtcNow);

    incidents[incident.IncidentId] = incident;
    return Results.Created($"/api/incidents/{incident.IncidentId}", incident);
});

app.MapPatch("/api/incidents/{incidentId:guid}/status", (
    Guid incidentId,
    UpdateIncidentStatusRequest request,
    ConcurrentDictionary<Guid, IncidentCase> incidents) =>
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

public record OpenIncidentRequest(Guid TrackId, Guid ThreatAssessmentId, string Zone);
public record UpdateIncidentStatusRequest(IncidentStatus Status);
