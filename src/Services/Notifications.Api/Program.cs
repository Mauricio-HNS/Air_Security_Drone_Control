using System.Collections.Concurrent;
using AirSecurityDroneControl.BuildingBlocks.Contracts;
using AirSecurityDroneControl.BuildingBlocks.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ConcurrentDictionary<Guid, NotificationMessage>>();
var dataDir = Path.Combine(Directory.GetCurrentDirectory(), ".runtime", "data", "notifications");
builder.Services.AddSingleton(new JsonFileStore<NotificationMessage>(dataDir, "notifications.json"));
builder.Services.AddSingleton(new EventLog(dataDir));

var app = builder.Build();
SeedData(app.Services);

app.MapGet("/", () => Results.Ok(new
{
    service = "Notifications.Api",
    status = "running",
    utc = DateTimeOffset.UtcNow
}));

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/api/notifications/send", async (
    SendNotificationRequest request,
    ConcurrentDictionary<Guid, NotificationMessage> notifications,
    JsonFileStore<NotificationMessage> store,
    EventLog eventLog,
    CancellationToken ct) =>
{
    var message = new NotificationMessage(
        NotificationId: Guid.NewGuid(),
        IncidentId: request.IncidentId,
        Channel: request.Channel,
        Severity: request.Severity,
        Target: request.Target,
        Message: request.Message,
        SentAtUtc: DateTimeOffset.UtcNow);

    notifications[message.NotificationId] = message;
    await store.WriteAllAsync(notifications.Values.OrderByDescending(x => x.SentAtUtc), ct);
    await eventLog.AppendAsync(
        new DomainEvent(Guid.NewGuid(), "NotificationSent", DateTimeOffset.UtcNow, message), ct);

    return Results.Accepted($"/api/notifications/{message.NotificationId}", message);
});

app.MapGet("/api/notifications", (ConcurrentDictionary<Guid, NotificationMessage> notifications, int limit = 100) =>
{
    var list = notifications.Values
        .OrderByDescending(x => x.SentAtUtc)
        .Take(Math.Clamp(limit, 1, 1000))
        .ToArray();
    return Results.Ok(list);
});

app.Run();

static void SeedData(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var notifications = scope.ServiceProvider.GetRequiredService<ConcurrentDictionary<Guid, NotificationMessage>>();
    var store = scope.ServiceProvider.GetRequiredService<JsonFileStore<NotificationMessage>>();

    foreach (var item in store.ReadAllAsync().GetAwaiter().GetResult())
    {
        notifications[item.NotificationId] = item;
    }
}

public record SendNotificationRequest(Guid IncidentId, string Channel, string Severity, string Target, string Message);
