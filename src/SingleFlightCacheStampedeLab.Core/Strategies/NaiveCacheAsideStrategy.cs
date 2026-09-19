using SingleFlightCacheStampedeLab.Abstractions;
using SingleFlightCacheStampedeLab.Backend;
using SingleFlightCacheStampedeLab.Cache;

namespace SingleFlightCacheStampedeLab.Strategies;

public sealed class NaiveCacheAsideStrategy(
    IBackendLoader backend, InMemoryCacheStore store, TimeSpan ttl, TimeProvider? timeProvider = null)
    : CacheStrategyBase(backend, store, ttl, timeProvider)
{
    public override string Name => "NaiveCacheAside";

    public override async ValueTask<BackendValue> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        ValidateRequest(key, cancellationToken);
        if (TryGetFresh(key, out BackendValue? value))
        {
            Diagnostics.Hit();
            return value;
        }

        Diagnostics.Miss();
        value = await Backend.LoadAsync(key, cancellationToken);
        Cache(key, value);
        return value;
    }
}
