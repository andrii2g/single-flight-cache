using System.Globalization;
using System.Text;
using SingleFlightCacheStampedeLab.Sweeps;

namespace SingleFlightCacheStampedeLab.Reporting;

public static class CsvResultWriter
{
    public const string Header = "scenario,strategy,concurrency,backend_ms,iteration,actual_loads,expected_loads,duplicate_loads,amplification,throughput,p50_ms,p95_ms,p99_ms,max_ms,lock_contentions,unrelated_key_wait_ms,flights_created,coalesced_callers,peak_waiters,ttl_ms,request_count,successful_requests,cache_hits,cache_misses,peak_concurrent_loads,lock_acquisitions,lock_wait_ms,unrelated_key_contentions,coalescing_ratio,b_request_latency_ms,shuffle_seed,wave_offset_ms";

    public static async Task WriteAsync(string path, IReadOnlyList<RunRow> rows, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(rows);
        await using var writer = new StreamWriter(path, false, new UTF8Encoding(false));
        await writer.WriteLineAsync(Header.AsMemory(), token);
        foreach (RunRow row in rows)
        {
            var m = row.Metrics;
            string[] fields =
            [
                Escape(row.Scenario.ToString()), Escape(row.Strategy), N(row.Concurrency), N(row.BackendMs), N(row.Iteration),
                N(m.ActualLoads), N(m.ExpectedLoads), N(m.DuplicateLoads), N(m.Amplification), N(m.Throughput),
                N(m.P50Ms), N(m.P95Ms), N(m.P99Ms), N(m.MaxMs), N(m.LockContentions), N(m.UnrelatedKeyWaitMs),
                N(m.FlightsCreated), N(m.CoalescedCallers), N(m.PeakWaiters), N(row.TtlMs), N(m.RequestCount),
                N(m.SuccessfulRequests), N(m.CacheHits), N(m.CacheMisses), N(m.PeakConcurrentLoads),
                N(m.LockAcquisitions), N(m.LockWaitMs), N(m.UnrelatedKeyContentions), N(m.CoalescingRatio),
                N(m.BRequestLatencyMs), row.ShuffleSeed?.ToString(CultureInfo.InvariantCulture) ?? "", N(row.WaveOffsetMs)
            ];
            await writer.WriteLineAsync(string.Join(',', fields).AsMemory(), token);
        }
    }

    private static string N(double value) => value.ToString("G17", CultureInfo.InvariantCulture);
    private static string Escape(string value) => value.IndexOfAny([',', '"', '\r', '\n']) < 0
        ? value : "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
}
