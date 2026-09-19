namespace SingleFlightCacheStampedeLab.Metrics;

// Counts use double so an even-iteration median can retain a fractional midpoint.
public sealed record RunMetrics(
    double RequestCount,
    double SuccessfulRequests,
    double CacheHits,
    double CacheMisses,
    double ActualLoads,
    double ExpectedLoads,
    double DuplicateLoads,
    double Amplification,
    double PeakConcurrentLoads,
    double Throughput,
    double P50Ms,
    double P95Ms,
    double P99Ms,
    double MaxMs,
    double LockAcquisitions,
    double LockContentions,
    double LockWaitMs,
    double UnrelatedKeyContentions,
    double UnrelatedKeyWaitMs,
    double FlightsCreated,
    double CoalescedCallers,
    double CoalescingRatio,
    double PeakWaiters,
    double BRequestLatencyMs);
