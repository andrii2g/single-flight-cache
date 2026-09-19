using SingleFlightCacheStampedeLab.Workloads;

namespace SingleFlightCacheStampedeLab.Metrics;

public static class MetricsCalculator
{
    public static RunMetrics Calculate(ScenarioResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        RequestObservation[] successful = result.Observations.Where(item => item.Succeeded).ToArray();
        if (successful.Length == 0 || result.WallTime <= TimeSpan.Zero)
            throw new ArgumentException("Metrics require successful requests and positive wall time.", nameof(result));
        double[] latencies = successful.Select(item => item.Elapsed.TotalMilliseconds).ToArray();
        var diagnostics = result.Diagnostics;
        return new(
            result.Observations.Count, successful.Length, diagnostics.CacheHits, diagnostics.CacheMisses,
            result.Backend.TotalLoads, result.Plan.ExpectedLogicalLoads,
            Math.Max(0, result.Backend.TotalLoads - result.Plan.ExpectedLogicalLoads),
            (double)result.Backend.TotalLoads / Math.Max(1, result.Plan.ExpectedLogicalLoads),
            result.Backend.PeakConcurrentLoads, successful.Length / result.WallTime.TotalSeconds,
            Percentiles.NearestRank(latencies, 50), Percentiles.NearestRank(latencies, 95),
            Percentiles.NearestRank(latencies, 99), latencies.Max(),
            diagnostics.LockAcquisitions, diagnostics.ContendedLockAcquisitions, diagnostics.TotalLockWait.TotalMilliseconds,
            diagnostics.UnrelatedKeyContentions, diagnostics.UnrelatedKeyWait.TotalMilliseconds,
            diagnostics.FlightsCreated, diagnostics.CoalescedCallers,
            (double)diagnostics.CoalescedCallers / Math.Max(1, diagnostics.CacheMisses),
            diagnostics.PeakFlightWaiters,
            successful.FirstOrDefault(item => item.Key == "fast-B")?.Elapsed.TotalMilliseconds ?? 0);
    }

    public static RunMetrics Median(IReadOnlyList<RunMetrics> runs)
    {
        ArgumentNullException.ThrowIfNull(runs);
        double M(Func<RunMetrics, double> select) => Percentiles.Median(runs.Select(select));
        return new(M(r => r.RequestCount), M(r => r.SuccessfulRequests), M(r => r.CacheHits), M(r => r.CacheMisses),
            M(r => r.ActualLoads), M(r => r.ExpectedLoads), M(r => r.DuplicateLoads), M(r => r.Amplification),
            M(r => r.PeakConcurrentLoads), M(r => r.Throughput), M(r => r.P50Ms), M(r => r.P95Ms),
            M(r => r.P99Ms), M(r => r.MaxMs), M(r => r.LockAcquisitions), M(r => r.LockContentions),
            M(r => r.LockWaitMs), M(r => r.UnrelatedKeyContentions), M(r => r.UnrelatedKeyWaitMs),
            M(r => r.FlightsCreated), M(r => r.CoalescedCallers), M(r => r.CoalescingRatio),
            M(r => r.PeakWaiters), M(r => r.BRequestLatencyMs));
    }
}
