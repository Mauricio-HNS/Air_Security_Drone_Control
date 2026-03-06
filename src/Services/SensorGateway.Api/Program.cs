using System.Collections.Concurrent;
using DroneShield.BuildingBlocks.Contracts;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ConcurrentDictionary<Guid, DetectionEvent>>();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    service = "SensorGateway.Api",
    status = "running",
    utc = DateTimeOffset.UtcNow
}));

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/api/sensors/detections", (
    CreateDetectionRequest request,
    ConcurrentDictionary<Guid, DetectionEvent> store) =>
{
    var detection = new DetectionEvent(
        DetectionId: Guid.NewGuid(),
        SensorNodeId: request.SensorNodeId,
        SensorType: request.SensorType,
        Classification: request.Classification,
        Position: new GeoPoint(request.Latitude, request.Longitude, request.AltitudeMeters),
        TimestampUtc: DateTimeOffset.UtcNow,
        Confidence: Math.Clamp(request.Confidence, 0, 1),
        HeadingDegrees: request.HeadingDegrees,
        SpeedMps: request.SpeedMps);

    store[detection.DetectionId] = detection;
    return Results.Created($"/api/sensors/detections/{detection.DetectionId}", detection);
});

app.MapGet("/api/sensors/detections", (
    ConcurrentDictionary<Guid, DetectionEvent> store,
    int limit = 100) =>
{
    var data = store.Values
        .OrderByDescending(d => d.TimestampUtc)
        .Take(Math.Clamp(limit, 1, 1000))
        .ToArray();

    return Results.Ok(data);
});

app.Run();

public record CreateDetectionRequest(
    string SensorNodeId,
    SensorType SensorType,
    ClassificationType Classification,
    double Latitude,
    double Longitude,
    double? AltitudeMeters,
    double Confidence,
    double? HeadingDegrees,
    double? SpeedMps);
