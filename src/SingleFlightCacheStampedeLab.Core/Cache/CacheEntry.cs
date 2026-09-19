namespace SingleFlightCacheStampedeLab.Cache;

internal sealed record CacheEntry<T>(T Value, DateTimeOffset ExpiresAt);
