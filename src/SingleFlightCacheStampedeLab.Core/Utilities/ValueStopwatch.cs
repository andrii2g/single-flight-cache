using System.Diagnostics;

namespace SingleFlightCacheStampedeLab.Utilities;

internal readonly struct ValueStopwatch
{
    private readonly long _started;

    private ValueStopwatch(long started) => _started = started;
    public static ValueStopwatch StartNew() => new(Stopwatch.GetTimestamp());
    public TimeSpan Elapsed => Stopwatch.GetElapsedTime(_started);
}
