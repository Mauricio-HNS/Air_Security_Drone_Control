using System.Collections.Concurrent;
using AirSecurityDroneControl.BuildingBlocks.Contracts;
using AirSecurityDroneControl.BuildingBlocks.Infrastructure;
using AirSecurityDroneControl.BuildingBlocks.Observability;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ConcurrentDictionary<Guid, NotificationMessage>>();
builder.Services.AddSingleton<BasicMetrics>();
var dataDir = Path.Combine(Directory.GetCurrentDirectory(), ".runtime", "data", "notifications");
builder.Services.AddSingleton(new JsonFileStore<NotificationMessage>(dataDir, "notifications.json"));
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
    service = "Notifications.Api",
    status = "running",
    utc = DateTimeOffset.UtcNow
}));

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/metrics/basic", (BasicMetrics metrics) => Results.Ok(metrics.Snapshot()));

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

public record SendNotificationRequest(Guid IncidentId, string Channel, string Severity, string Target, string Message);
