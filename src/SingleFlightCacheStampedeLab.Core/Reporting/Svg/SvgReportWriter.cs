using SingleFlightCacheStampedeLab.Metrics;
using SingleFlightCacheStampedeLab.Sweeps;
using SingleFlightCacheStampedeLab.Workloads;

namespace SingleFlightCacheStampedeLab.Reporting.Svg;

public static class SvgReportWriter
{
    public static async Task WriteAsync(string directory, IReadOnlyList<RunSummary> summaries, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        ArgumentNullException.ThrowIfNull(summaries);
        Directory.CreateDirectory(directory);
        RunSummary[] hot = summaries.Where(row => row.Scenario == ScenarioKind.HotKeyBurst).ToArray();
        double cost = hot.Select(row => row.BackendMs).DefaultIfEmpty(0).Max();
        RunSummary[] selected = hot.Where(row => row.BackendMs == cost).ToArray();
        string subtitle = FormattableString.Invariant($"HotKeyBurst | backend {cost:0.###} ms | medians across iterations");
        var amplification = new LineChart("Backend load amplification", "Request concurrency", "Physical / logical loads",
            Series(selected, row => row.Concurrency, metrics => metrics.Amplification), subtitle);
        var latency = new LineChart("HotKeyBurst: P95 request latency", "Request concurrency", "P95 latency (ms)",
            Series(selected, row => row.Concurrency, metrics => metrics.P95Ms), subtitle);
        RunSummary[] blocking = summaries.Where(row => row.Scenario == ScenarioKind.UnrelatedKeyBlocking).ToArray();
        var unrelated = new LineChart("Unrelated-key blocking: request B", "Backend cost (ms)", "B request latency (ms)",
            Series(blocking, row => row.BackendMs, metrics => metrics.BRequestLatencyMs),
            "A starts first; B follows after min(5 ms, backend cost / 10). Both keys use the same backend cost.");
        await File.WriteAllTextAsync(Path.Combine(directory, "backend-amplification.svg"), amplification.Render(), token);
        await File.WriteAllTextAsync(Path.Combine(directory, "p95-latency-vs-concurrency.svg"), latency.Render(), token);
        await File.WriteAllTextAsync(Path.Combine(directory, "unrelated-key-blocking.svg"), unrelated.Render(), token);
    }

    private static IReadOnlyList<ChartSeries> Series(
        IEnumerable<RunSummary> summaries, Func<RunSummary, double> x, Func<RunMetrics, double> y)
        => summaries.GroupBy(row => row.Strategy).Select(group => new ChartSeries(group.Key,
            group.OrderBy(x).Select(row => new ChartPoint(x(row), y(row.Median))).ToArray())).ToArray();
}
