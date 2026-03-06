using System.Collections.Concurrent;
using AirSecurityDroneControl.BuildingBlocks.Contracts;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ConcurrentDictionary<Guid, DetectionEvent>>();
builder.Services.AddSingleton<ConcurrentDictionary<string, SensorNodeStatus>>();

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
    ConcurrentDictionary<Guid, DetectionEvent> store,
    ConcurrentDictionary<string, SensorNodeStatus> sensorStatus) =>
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
    sensorStatus[detection.SensorNodeId] = new SensorNodeStatus(
        SensorNodeId: detection.SensorNodeId,
        SensorType: detection.SensorType,
        Health: SensorNodeHealth.Online,
        LastSeenUtc: detection.TimestampUtc,
        SignalQuality: Math.Round(detection.Confidence * 100, 2));

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

app.MapGet("/api/sensors/status", (
    ConcurrentDictionary<string, SensorNodeStatus> sensorStatus) =>
{
    var statuses = sensorStatus.Values
        .OrderByDescending(x => x.LastSeenUtc)
        .ToArray();

    return Results.Ok(statuses);
});

app.MapGet("/api/sensors/status/{sensorNodeId}", (
    string sensorNodeId,
    ConcurrentDictionary<string, SensorNodeStatus> sensorStatus) =>
{
    if (!sensorStatus.TryGetValue(sensorNodeId, out var status))
    {
        return Results.NotFound();
    }

    return Results.Ok(status);
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
