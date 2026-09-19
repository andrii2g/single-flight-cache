namespace SingleFlightCacheStampedeLab.Backend;

public sealed record BackendLoadEvent(
    long AttemptId, string Key, long Generation, DateTimeOffset StartedAt, TimeSpan Duration);
