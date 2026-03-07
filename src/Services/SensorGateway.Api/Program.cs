using System.Collections.Concurrent;
using AirSecurityDroneControl.BuildingBlocks.Contracts;
using AirSecurityDroneControl.BuildingBlocks.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ConcurrentDictionary<Guid, DetectionEvent>>();
builder.Services.AddSingleton<ConcurrentDictionary<string, SensorNodeStatus>>();

var dataDir = Path.Combine(Directory.GetCurrentDirectory(), ".runtime", "data", "sensor-gateway");
builder.Services.AddSingleton(new JsonFileStore<DetectionEvent>(dataDir, "detections.json"));
builder.Services.AddSingleton(new JsonFileStore<SensorNodeStatus>(dataDir, "sensor-status.json"));
builder.Services.AddSingleton(new EventLog(dataDir));

var app = builder.Build();

SeedData(app.Services);

app.MapGet("/", () => Results.Ok(new
{
    service = "SensorGateway.Api",
    status = "running",
    utc = DateTimeOffset.UtcNow
}));

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/api/sensors/detections", async (
    CreateDetectionRequest request,
    ConcurrentDictionary<Guid, DetectionEvent> store,
    ConcurrentDictionary<string, SensorNodeStatus> sensorStatus,
    JsonFileStore<DetectionEvent> detectionStore,
    JsonFileStore<SensorNodeStatus> sensorStore,
    EventLog eventLog,
    CancellationToken ct) =>
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

    await detectionStore.WriteAllAsync(store.Values.OrderByDescending(x => x.TimestampUtc), ct);
    await sensorStore.WriteAllAsync(sensorStatus.Values.OrderByDescending(x => x.LastSeenUtc), ct);
    await eventLog.AppendAsync(
        new DomainEvent(Guid.NewGuid(), "DetectionReceived", DateTimeOffset.UtcNow, detection), ct);

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

static void SeedData(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var detections = scope.ServiceProvider.GetRequiredService<ConcurrentDictionary<Guid, DetectionEvent>>();
    var sensorStatuses = scope.ServiceProvider.GetRequiredService<ConcurrentDictionary<string, SensorNodeStatus>>();
    var detectionStore = scope.ServiceProvider.GetRequiredService<JsonFileStore<DetectionEvent>>();
    var sensorStore = scope.ServiceProvider.GetRequiredService<JsonFileStore<SensorNodeStatus>>();

    foreach (var detection in detectionStore.ReadAllAsync().GetAwaiter().GetResult())
    {
        detections[detection.DetectionId] = detection;
    }

    foreach (var status in sensorStore.ReadAllAsync().GetAwaiter().GetResult())
    {
        sensorStatuses[status.SensorNodeId] = status;
    }
}

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
