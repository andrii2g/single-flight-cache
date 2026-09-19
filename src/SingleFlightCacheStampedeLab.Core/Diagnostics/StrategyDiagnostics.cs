namespace SingleFlightCacheStampedeLab.Diagnostics;

internal sealed class StrategyDiagnostics
{
    private long _hits, _misses, _acquisitions, _contentions, _waitTicks;
    private long _unrelatedContentions, _unrelatedTicks, _flights, _coalesced, _waiters, _peakWaiters;

    public void Hit() => Interlocked.Increment(ref _hits);
    public void Miss() => Interlocked.Increment(ref _misses);
    public void LockAcquired() => Interlocked.Increment(ref _acquisitions);
    public void FlightCreated() => Interlocked.Increment(ref _flights);

    public void LockContended(bool unrelated)
    {
        Interlocked.Increment(ref _contentions);
        if (unrelated) Interlocked.Increment(ref _unrelatedContentions);
    }

    public void LockWaited(TimeSpan elapsed, bool unrelated)
    {
        Interlocked.Add(ref _waitTicks, elapsed.Ticks);
        if (unrelated) Interlocked.Add(ref _unrelatedTicks, elapsed.Ticks);
    }

    public void WaiterJoined()
    {
        Interlocked.Increment(ref _coalesced);
        long current = Interlocked.Increment(ref _waiters);
        long peak;
        while (current > (peak = Interlocked.Read(ref _peakWaiters)) &&
               Interlocked.CompareExchange(ref _peakWaiters, current, peak) != peak)
        {
        }
    }

    public void WaiterLeft() => Interlocked.Decrement(ref _waiters);

    public StrategyDiagnosticsSnapshot Snapshot() => new(
        Interlocked.Read(ref _hits), Interlocked.Read(ref _misses),
        Interlocked.Read(ref _acquisitions), Interlocked.Read(ref _contentions),
        Interlocked.Read(ref _waitTicks), Interlocked.Read(ref _unrelatedContentions),
        Interlocked.Read(ref _unrelatedTicks), Interlocked.Read(ref _flights),
        Interlocked.Read(ref _coalesced), Interlocked.Read(ref _waiters),
        Interlocked.Read(ref _peakWaiters));
}
