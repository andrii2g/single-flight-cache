namespace SingleFlightCacheStampedeLab.Tests.TestDoubles;

internal sealed class ManualTimeProvider : TimeProvider
{
    private long _ticks = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero).Ticks;

    public override DateTimeOffset GetUtcNow() => new(Interlocked.Read(ref _ticks), TimeSpan.Zero);

    public void Advance(TimeSpan elapsed)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(elapsed, TimeSpan.Zero);
        Interlocked.Add(ref _ticks, elapsed.Ticks);
    }
}
