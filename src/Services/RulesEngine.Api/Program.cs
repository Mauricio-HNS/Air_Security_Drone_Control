using System.Collections.Concurrent;
using AirSecurityDroneControl.BuildingBlocks.Contracts;
using AirSecurityDroneControl.BuildingBlocks.Infrastructure;
using AirSecurityDroneControl.BuildingBlocks.Observability;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ConcurrentDictionary<Guid, RulePolicy>>();
builder.Services.AddSingleton<BasicMetrics>();
var dataDir = Path.Combine(Directory.GetCurrentDirectory(), ".runtime", "data", "rules-engine");
builder.Services.AddSingleton(new JsonFileStore<RulePolicy>(dataDir, "policies.json"));
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

    var adminOnly = context.Request.Path.StartsWithSegments("/api/rules")
        && context.Request.Method == HttpMethods.Post
        && !context.Request.Path.StartsWithSegments("/api/rules/simulate");

    if (!HasAccess(context, app.Configuration, adminOnly))
    {
        return;
    }

    await next();
});
SeedData(app.Services);

app.MapGet("/", () => Results.Ok(new
{
    service = "RulesEngine.Api",
    status = "running",
    utc = DateTimeOffset.UtcNow
}));

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/metrics/basic", (BasicMetrics metrics) => Results.Ok(metrics.Snapshot()));

app.MapPost("/api/rules", async (
    CreateRuleRequest request,
    ConcurrentDictionary<Guid, RulePolicy> policies,
    JsonFileStore<RulePolicy> store,
    EventLog eventLog,
    CancellationToken ct) =>
{
    var policy = new RulePolicy(
        RuleId: Guid.NewGuid(),
        Name: request.Name,
        Zone: request.Zone,
        MinThreatScore: Math.Clamp(request.MinThreatScore, 0, 100),
        MinDetections: Math.Max(1, request.MinDetections),
        Enabled: request.Enabled);

    policies[policy.RuleId] = policy;
    await store.WriteAllAsync(policies.Values.OrderBy(x => x.Name), ct);
    await eventLog.AppendAsync(new DomainEvent(Guid.NewGuid(), "RuleCreated", DateTimeOffset.UtcNow, policy), ct);
    return Results.Created($"/api/rules/{policy.RuleId}", policy);
});

app.MapGet("/api/rules", (ConcurrentDictionary<Guid, RulePolicy> policies) =>
{
    return Results.Ok(policies.Values.OrderBy(x => x.Name).ToArray());
});

app.MapPost("/api/rules/simulate", (
    SimulateRuleRequest request,
    ConcurrentDictionary<Guid, RulePolicy> policies) =>
{
    var zonePolicies = policies.Values
        .Where(p => p.Enabled && string.Equals(p.Zone, request.Zone, StringComparison.OrdinalIgnoreCase))
        .ToArray();

    if (zonePolicies.Length == 0)
    {
        return Results.Ok(new RuleSimulationResult("OBSERVATION", "No active policy for zone."));
    }

    var matched = zonePolicies
        .Where(x => request.ThreatScore >= x.MinThreatScore && request.DetectionCount >= x.MinDetections)
        .OrderByDescending(x => x.MinThreatScore)
        .FirstOrDefault();

    if (matched is null)
    {
        return Results.Ok(new RuleSimulationResult("OBSERVATION", "Below thresholds."));
    }

    var action = request.ThreatScore >= 80 ? "RED_ALERT" : "YELLOW_ALERT";
    return Results.Ok(new RuleSimulationResult(action, $"Matched policy: {matched.Name}"));
});

app.Run();

static void SeedData(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var policies = scope.ServiceProvider.GetRequiredService<ConcurrentDictionary<Guid, RulePolicy>>();
    var store = scope.ServiceProvider.GetRequiredService<JsonFileStore<RulePolicy>>();

    foreach (var policy in store.ReadAllAsync().GetAwaiter().GetResult())
    {
        policies[policy.RuleId] = policy;
    }
}

static bool HasAccess(HttpContext context, IConfiguration configuration, bool adminOnly)
{
    var key = context.Request.Headers["X-API-Key"].FirstOrDefault();
    if (!string.Equals(key, configuration["Security:ApiKey"], StringComparison.Ordinal))
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return false;
    }

    var role = context.Request.Headers["X-Role"].FirstOrDefault();
    var isAdmin = string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase);
    var isOperator = string.Equals(role, "operator", StringComparison.OrdinalIgnoreCase);

    var ok = adminOnly ? isAdmin : (isAdmin || isOperator);
    if (!ok)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return false;
    }

    return true;
}

public record CreateRuleRequest(string Name, string Zone, double MinThreatScore, int MinDetections, bool Enabled = true);
public record SimulateRuleRequest(string Zone, double ThreatScore, int DetectionCount);
public record RuleSimulationResult(string Action, string Reason);
