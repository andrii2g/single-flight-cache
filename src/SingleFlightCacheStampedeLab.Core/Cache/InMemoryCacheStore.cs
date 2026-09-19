using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using SingleFlightCacheStampedeLab.Backend;

namespace SingleFlightCacheStampedeLab.Cache;

public sealed class InMemoryCacheStore
{
    private readonly ConcurrentDictionary<string, CacheEntry<BackendValue>> _entries = new(StringComparer.Ordinal);

    public bool TryGetFresh(string key, DateTimeOffset now, [NotNullWhen(true)] out BackendValue? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (_entries.TryGetValue(key, out CacheEntry<BackendValue>? entry))
        {
            if (now < entry.ExpiresAt)
            {
                value = entry.Value;
                return true;
            }

            // An expired observation must not evict a concurrently refreshed entry.
            ((ICollection<KeyValuePair<string, CacheEntry<BackendValue>>>)_entries).Remove(new(key, entry));
        }

        value = null;
        return false;
    }

    public void Set(string key, BackendValue value, DateTimeOffset expiresAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);
        _entries[key] = new(value, expiresAt);
    }

    public bool Expire(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return _entries.TryRemove(key, out _);
    }

    public void Clear() => _entries.Clear();
}
