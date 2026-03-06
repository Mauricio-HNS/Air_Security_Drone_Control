using System.Collections.Concurrent;
using AirSecurityCity.BuildingBlocks.Contracts;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ConcurrentDictionary<Guid, FusedTrack>>();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    service = "Fusion.Api",
    status = "running",
    utc = DateTimeOffset.UtcNow
}));

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/api/fusion/fuse", (
    FuseDetectionsRequest request,
    ConcurrentDictionary<Guid, FusedTrack> tracks) =>
{
    if (request.Detections.Count == 0)
    {
        return Results.BadRequest("At least one detection is required.");
    }

    var avgLat = request.Detections.Average(x => x.Position.Latitude);
    var avgLon = request.Detections.Average(x => x.Position.Longitude);
    var avgAlt = request.Detections
        .Where(x => x.Position.AltitudeMeters.HasValue)
        .Select(x => x.Position.AltitudeMeters!.Value)
        .DefaultIfEmpty(0)
        .Average();

    var avgSpeed = request.Detections
        .Where(x => x.SpeedMps.HasValue)
        .Select(x => x.SpeedMps!.Value)
        .DefaultIfEmpty(0)
        .Average();

    var avgHeading = request.Detections
        .Where(x => x.HeadingDegrees.HasValue)
        .Select(x => x.HeadingDegrees!.Value)
        .DefaultIfEmpty(0)
        .Average();

    var confidence = request.Detections.Average(x => x.Confidence);

    var track = new FusedTrack(
        TrackId: Guid.NewGuid(),
        DetectionIds: request.Detections.Select(d => d.DetectionId).ToArray(),
        EstimatedPosition: new GeoPoint(avgLat, avgLon, avgAlt),
        EstimatedSpeedMps: avgSpeed,
        EstimatedHeadingDegrees: avgHeading,
        Confidence: confidence,
        LastUpdateUtc: DateTimeOffset.UtcNow);

    tracks[track.TrackId] = track;
    return Results.Ok(track);
});

app.MapGet("/api/fusion/tracks", (
    ConcurrentDictionary<Guid, FusedTrack> tracks,
    int limit = 100) =>
{
    var data = tracks.Values
        .OrderByDescending(x => x.LastUpdateUtc)
        .Take(Math.Clamp(limit, 1, 1000))
        .ToArray();

    return Results.Ok(data);
});

app.Run();

public record FuseDetectionsRequest(IReadOnlyCollection<DetectionEvent> Detections);
