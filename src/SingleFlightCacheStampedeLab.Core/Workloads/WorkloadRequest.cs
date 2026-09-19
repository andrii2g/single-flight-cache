namespace SingleFlightCacheStampedeLab.Workloads;

public sealed record WorkloadRequest(int Index, string Key, TimeSpan DelayAfterRelease = default);
