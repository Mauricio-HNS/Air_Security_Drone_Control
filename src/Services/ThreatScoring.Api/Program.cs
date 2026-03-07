using System.Collections.Concurrent;
using AirSecurityDroneControl.BuildingBlocks.Contracts;
using AirSecurityDroneControl.BuildingBlocks.Infrastructure;
using AirSecurityDroneControl.BuildingBlocks.Observability;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ConcurrentDictionary<Guid, ThreatAssessment>>();
builder.Services.AddSingleton<BasicMetrics>();
var dataDir = Path.Combine(Directory.GetCurrentDirectory(), ".runtime", "data", "threat-scoring");
builder.Services.AddSingleton(new JsonFileStore<ThreatAssessment>(dataDir, "assessments.json"));
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
    service = "ThreatScoring.Api",
    status = "running",
    utc = DateTimeOffset.UtcNow
}));

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/metrics/basic", (BasicMetrics metrics) => Results.Ok(metrics.Snapshot()));

app.MapPost("/api/threat/assess", async (
    AssessThreatRequest request,
    ConcurrentDictionary<Guid, ThreatAssessment> store,
    JsonFileStore<ThreatAssessment> assessmentStore,
    EventLog eventLog,
    CancellationToken ct) =>
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
    await assessmentStore.WriteAllAsync(store.Values.OrderByDescending(x => x.TimestampUtc), ct);
    await eventLog.AppendAsync(
        new DomainEvent(Guid.NewGuid(), "ThreatAssessed", DateTimeOffset.UtcNow, assessment), ct);
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

static void SeedData(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var items = scope.ServiceProvider.GetRequiredService<ConcurrentDictionary<Guid, ThreatAssessment>>();
    var store = scope.ServiceProvider.GetRequiredService<JsonFileStore<ThreatAssessment>>();

    foreach (var assessment in store.ReadAllAsync().GetAwaiter().GetResult())
    {
        items[assessment.AssessmentId] = assessment;
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
