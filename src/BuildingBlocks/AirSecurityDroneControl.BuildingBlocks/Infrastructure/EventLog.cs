using System.Text.Json;

namespace AirSecurityDroneControl.BuildingBlocks.Infrastructure;

public sealed record DomainEvent(
    Guid EventId,
    string EventType,
    DateTimeOffset TimestampUtc,
    object Payload);

public sealed class EventLog
{
    private readonly string _logPath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public EventLog(string baseDir, string fileName = "events.log")
    {
        Directory.CreateDirectory(baseDir);
        _logPath = Path.Combine(baseDir, fileName);
    }

    public async Task AppendAsync(DomainEvent @event, CancellationToken ct = default)
    {
        var line = JsonSerializer.Serialize(@event);
        await _lock.WaitAsync(ct);
        try
        {
            await File.AppendAllTextAsync(_logPath, line + Environment.NewLine, ct);
        }
        finally
        {
            _lock.Release();
        }
    }
}
