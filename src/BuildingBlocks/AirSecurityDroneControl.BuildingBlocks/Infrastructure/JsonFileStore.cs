using System.Text.Json;

namespace AirSecurityDroneControl.BuildingBlocks.Infrastructure;

public sealed class JsonFileStore<T>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _filePath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public JsonFileStore(string baseDir, string fileName)
    {
        Directory.CreateDirectory(baseDir);
        _filePath = Path.Combine(baseDir, fileName);
    }

    public async Task<IReadOnlyList<T>> ReadAllAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            if (!File.Exists(_filePath))
            {
                return Array.Empty<T>();
            }

            await using var stream = File.OpenRead(_filePath);
            var data = await JsonSerializer.DeserializeAsync<List<T>>(stream, JsonOptions, ct);
            return data is null ? Array.Empty<T>() : data;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task WriteAllAsync(IEnumerable<T> items, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var tempFile = $"{_filePath}.tmp";
            await using (var stream = File.Create(tempFile))
            {
                await JsonSerializer.SerializeAsync(stream, items, JsonOptions, ct);
            }

            File.Move(tempFile, _filePath, overwrite: true);
        }
        finally
        {
            _lock.Release();
        }
    }
}
