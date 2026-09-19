namespace SingleFlightCacheStampedeLab.Diagnostics;

public sealed record StrategyDiagnosticsSnapshot(
    long CacheHits = 0,
    long CacheMisses = 0,
    long LockAcquisitions = 0,
    long ContendedLockAcquisitions = 0,
    long TotalLockWaitTicks = 0,
    long UnrelatedKeyContentions = 0,
    long UnrelatedKeyWaitTicks = 0,
    long FlightsCreated = 0,
    long CoalescedCallers = 0,
    long CurrentFlightWaiters = 0,
    long PeakFlightWaiters = 0)
{
    public TimeSpan TotalLockWait => TimeSpan.FromTicks(TotalLockWaitTicks);
    public TimeSpan UnrelatedKeyWait => TimeSpan.FromTicks(UnrelatedKeyWaitTicks);
}
