namespace SingleFlightCacheStampedeLab.Diagnostics;

public sealed record BackendDiagnosticsSnapshot(
    long TotalLoads,
    int CurrentConcurrentLoads,
    int PeakConcurrentLoads,
    IReadOnlyDictionary<string, long> LoadsByKey);
