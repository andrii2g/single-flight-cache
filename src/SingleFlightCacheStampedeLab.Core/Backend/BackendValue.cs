namespace SingleFlightCacheStampedeLab.Backend;

public sealed record BackendValue(string Key, long Generation, string Payload)
{
    public static BackendValue Create(string key, long generation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return new(key, generation, FormattableString.Invariant($"value:{key}:generation:{generation}"));
    }
}
