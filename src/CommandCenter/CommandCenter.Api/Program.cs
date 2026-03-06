using System.Collections.Concurrent;
using AirSecurityCity.BuildingBlocks.Contracts;

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
}
