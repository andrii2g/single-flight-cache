using SingleFlightCacheStampedeLab.Backend;

namespace SingleFlightCacheStampedeLab.Workloads;

public sealed record RequestObservation(
    int Index, string Key, BackendValue? Value, TimeSpan Elapsed,
    bool Canceled = false, string? Failure = null)
{
    public bool Succeeded => Value is not null && !Canceled && Failure is null;
}
