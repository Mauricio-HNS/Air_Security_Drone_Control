using System.Collections.Concurrent;
using DroneShield.BuildingBlocks.Contracts;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ConcurrentDictionary<Guid, ThreatAssessment>>();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    service = "ThreatScoring.Api",
    status = "running",
    utc = DateTimeOffset.UtcNow
}));

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/api/threat/assess", (
    AssessThreatRequest request,
    ConcurrentDictionary<Guid, ThreatAssessment> store) =>
{
    var score = ThreatScoreCalculator.Calculate(request.Track, request.Zone);
    var level = score switch
    {
        >= 80 => ThreatLevel.Critical,
        >= 60 => ThreatLevel.High,
        >= 35 => ThreatLevel.Medium,
        _ => ThreatLevel.Low
    };

    var summary =
        $"Track {request.Track.TrackId} scored {score:F1} ({level}) in zone {request.Zone.Name}.";

    var assessment = new ThreatAssessment(
        AssessmentId: Guid.NewGuid(),
        TrackId: request.Track.TrackId,
        Score: score,
        Level: level,
        Summary: summary,
        TimestampUtc: DateTimeOffset.UtcNow);

    store[assessment.AssessmentId] = assessment;
    return Results.Ok(assessment);
});

app.MapGet("/api/threat/assessments", (
    ConcurrentDictionary<Guid, ThreatAssessment> store,
    int limit = 100) =>
{
    var data = store.Values
        .OrderByDescending(x => x.TimestampUtc)
        .Take(Math.Clamp(limit, 1, 1000))
        .ToArray();

    return Results.Ok(data);
});

app.Run();

public record AssessThreatRequest(FusedTrack Track, ProtectedZone Zone);

public static class ThreatScoreCalculator
{
    public static double Calculate(FusedTrack track, ProtectedZone zone)
    {
        var distance = Geo.DistanceMeters(track.EstimatedPosition, zone.Center);

        var proximityFactor = distance <= 0 ? 1 : Math.Min(1, zone.RadiusMeters / distance);
        var confidenceFactor = Math.Clamp(track.Confidence, 0, 1);
        var speedFactor = Math.Min(track.EstimatedSpeedMps / 20d, 1);
        var sensitiveFactor = zone.Sensitive ? 0.2 : 0;

        var score = (proximityFactor * 45)
            + (confidenceFactor * 30)
            + (speedFactor * 15)
            + (sensitiveFactor * 100)
            + (track.DetectionIds.Count >= 2 ? 10 : 0);

        return Math.Round(Math.Clamp(score, 0, 100), 2);
    }
}

public static class Geo
{
    private const double EarthRadiusMeters = 6_371_000;

    public static double DistanceMeters(GeoPoint a, GeoPoint b)
    {
        var dLat = ToRadians(b.Latitude - a.Latitude);
        var dLon = ToRadians(b.Longitude - a.Longitude);
        var lat1 = ToRadians(a.Latitude);
        var lat2 = ToRadians(b.Latitude);

        var h = Math.Pow(Math.Sin(dLat / 2), 2) +
                Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin(dLon / 2), 2);

        return 2 * EarthRadiusMeters * Math.Asin(Math.Sqrt(h));
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;
}
