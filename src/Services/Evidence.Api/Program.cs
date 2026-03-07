using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using AirSecurityDroneControl.BuildingBlocks.Contracts;
using AirSecurityDroneControl.BuildingBlocks.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ConcurrentDictionary<Guid, EvidenceItem>>();
var dataDir = Path.Combine(Directory.GetCurrentDirectory(), ".runtime", "data", "evidence");
builder.Services.AddSingleton(new JsonFileStore<EvidenceItem>(dataDir, "evidence.json"));
builder.Services.AddSingleton(new EventLog(dataDir));

var app = builder.Build();
SeedData(app.Services);

app.MapGet("/", () => Results.Ok(new
{
    service = "Evidence.Api",
    status = "running",
    utc = DateTimeOffset.UtcNow
}));

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

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

public record CreateEvidenceRequest(Guid IncidentId, string EvidenceType, string Content);
