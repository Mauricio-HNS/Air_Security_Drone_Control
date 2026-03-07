using System.Collections.Concurrent;

namespace AirSecurityDroneControl.BuildingBlocks.Observability;

public sealed class BasicMetrics
{
    private long _totalRequests;
    private readonly ConcurrentDictionary<int, long> _statusCodes = new();
    private readonly ConcurrentDictionary<string, long> _routes = new();

    public void Record(string route, int statusCode)
    {
        Interlocked.Increment(ref _totalRequests);
        _statusCodes.AddOrUpdate(statusCode, 1, (_, value) => value + 1);
        _routes.AddOrUpdate(route, 1, (_, value) => value + 1);
    }

    public object Snapshot() => new
    {
        totalRequests = _totalRequests,
        statusCodes = _statusCodes.OrderBy(x => x.Key).ToDictionary(x => x.Key.ToString(), x => x.Value),
        routes = _routes.OrderByDescending(x => x.Value).Take(30).ToDictionary(x => x.Key, x => x.Value)
    };
}
