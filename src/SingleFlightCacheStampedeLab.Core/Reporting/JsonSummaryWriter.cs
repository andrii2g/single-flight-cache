using System.Text.Json;
using System.Text.Json.Serialization;
using SingleFlightCacheStampedeLab.Sweeps;

namespace SingleFlightCacheStampedeLab.Reporting;

public static class JsonSummaryWriter
{
    public static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    public static async Task WriteAsync(string path, ExperimentReport report, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(report);
        await using FileStream stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, report, CreateOptions(), token);
    }
}
