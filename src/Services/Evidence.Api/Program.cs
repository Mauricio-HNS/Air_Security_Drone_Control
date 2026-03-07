using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using AirSecurityDroneControl.BuildingBlocks.Contracts;
using AirSecurityDroneControl.BuildingBlocks.Infrastructure;
using AirSecurityDroneControl.BuildingBlocks.Observability;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ConcurrentDictionary<Guid, EvidenceItem>>();
builder.Services.AddSingleton<BasicMetrics>();
var dataDir = Path.Combine(Directory.GetCurrentDirectory(), ".runtime", "data", "evidence");
builder.Services.AddSingleton(new JsonFileStore<EvidenceItem>(dataDir, "evidence.json"));
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
    service = "Evidence.Api",
    status = "running",
    utc = DateTimeOffset.UtcNow
}));

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/metrics/basic", (BasicMetrics metrics) => Results.Ok(metrics.Snapshot()));

app.MapPost("/api/evidence", async (
    CreateEvidenceRequest request,
    ConcurrentDictionary<Guid, EvidenceItem> evidence,
    JsonFileStore<EvidenceItem> store,
    EventLog eventLog,
    CancellationToken ct) =>
{
    var hash = ComputeHash($"{request.IncidentId}|{request.EvidenceType}|{request.Content}");
    var item = new EvidenceItem(
        EvidenceId: Guid.NewGuid(),
        IncidentId: request.IncidentId,
        EvidenceType: request.EvidenceType,
        Content: request.Content,
        Hash: hash,
        CreatedAtUtc: DateTimeOffset.UtcNow);

    evidence[item.EvidenceId] = item;
    await store.WriteAllAsync(evidence.Values.OrderByDescending(x => x.CreatedAtUtc), ct);
    await eventLog.AppendAsync(new DomainEvent(Guid.NewGuid(), "EvidenceStored", DateTimeOffset.UtcNow, item), ct);
    return Results.Created($"/api/evidence/{item.EvidenceId}", item);
});

app.MapGet("/api/evidence", (ConcurrentDictionary<Guid, EvidenceItem> evidence, Guid? incidentId = null, int limit = 200) =>
{
    var data = evidence.Values.AsEnumerable();
    if (incidentId.HasValue)
    {
        data = data.Where(x => x.IncidentId == incidentId.Value);
    }

    return Results.Ok(data.OrderByDescending(x => x.CreatedAtUtc).Take(Math.Clamp(limit, 1, 1000)).ToArray());
});

app.Run();

static void SeedData(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var evidence = scope.ServiceProvider.GetRequiredService<ConcurrentDictionary<Guid, EvidenceItem>>();
    var store = scope.ServiceProvider.GetRequiredService<JsonFileStore<EvidenceItem>>();

    foreach (var item in store.ReadAllAsync().GetAwaiter().GetResult())
    {
        evidence[item.EvidenceId] = item;
    }
}

static string ComputeHash(string input)
{
    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
    return Convert.ToHexString(bytes);
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

public record CreateEvidenceRequest(Guid IncidentId, string EvidenceType, string Content);
