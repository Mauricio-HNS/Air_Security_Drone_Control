using System.Collections.Concurrent;
using AirSecurityDroneControl.BuildingBlocks.Contracts;
using AirSecurityDroneControl.BuildingBlocks.Infrastructure;
using AirSecurityDroneControl.BuildingBlocks.Observability;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ConcurrentDictionary<Guid, FusedTrack>>();
builder.Services.AddSingleton<BasicMetrics>();
var dataDir = Path.Combine(Directory.GetCurrentDirectory(), ".runtime", "data", "fusion");
builder.Services.AddSingleton(new JsonFileStore<FusedTrack>(dataDir, "tracks.json"));
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
    service = "Fusion.Api",
    status = "running",
    utc = DateTimeOffset.UtcNow
}));

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/metrics/basic", (BasicMetrics metrics) => Results.Ok(metrics.Snapshot()));

app.MapPost("/api/fusion/fuse", async (
    FuseDetectionsRequest request,
    ConcurrentDictionary<Guid, FusedTrack> tracks,
    JsonFileStore<FusedTrack> trackStore,
    EventLog eventLog,
    CancellationToken ct) =>
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
    await trackStore.WriteAllAsync(tracks.Values.OrderByDescending(x => x.LastUpdateUtc), ct);
    await eventLog.AppendAsync(
        new DomainEvent(Guid.NewGuid(), "TrackFused", DateTimeOffset.UtcNow, track), ct);
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

static void SeedData(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var tracks = scope.ServiceProvider.GetRequiredService<ConcurrentDictionary<Guid, FusedTrack>>();
    var store = scope.ServiceProvider.GetRequiredService<JsonFileStore<FusedTrack>>();

    foreach (var track in store.ReadAllAsync().GetAwaiter().GetResult())
    {
        tracks[track.TrackId] = track;
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

public record FuseDetectionsRequest(IReadOnlyCollection<DetectionEvent> Detections);
