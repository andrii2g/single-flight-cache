using SingleFlightCacheStampedeLab.Abstractions;
using SingleFlightCacheStampedeLab.Backend;
using SingleFlightCacheStampedeLab.Cache;
using SingleFlightCacheStampedeLab.Utilities;

namespace SingleFlightCacheStampedeLab.Strategies;

public sealed class GlobalLockCacheStrategy(
    IBackendLoader backend, InMemoryCacheStore store, TimeSpan ttl, TimeProvider? timeProvider = null)
    : CacheStrategyBase(backend, store, ttl, timeProvider), IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private string? _holderKey;

    public override string Name => "GlobalLock";

    public override async ValueTask<BackendValue> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        ValidateRequest(key, cancellationToken);
        if (TryGetFresh(key, out BackendValue? value))
        {
            Diagnostics.Hit();
            return value;
        }

        Diagnostics.Miss();
        if (!_gate.Wait(0))
        {
            string? holder = Volatile.Read(ref _holderKey);
            bool unrelated = holder is not null && !StringComparer.Ordinal.Equals(holder, key);
            Diagnostics.LockContended(unrelated);
            ValueStopwatch timer = ValueStopwatch.StartNew();
            try
            {
                await _gate.WaitAsync(cancellationToken);
            }
            finally
            {
                // Include abandoned waits; contention means the immediate acquisition failed.
                Diagnostics.LockWaited(timer.Elapsed, unrelated);
            }
        }

        Diagnostics.LockAcquired();
        Volatile.Write(ref _holderKey, key);
        try
        {
            if (TryGetFresh(key, out value)) return value;
            value = await Backend.LoadAsync(key, cancellationToken);
            Cache(key, value);
            return value;
        }
        finally
        {
            Volatile.Write(ref _holderKey, null);
            _gate.Release();
        }
    }

    /// <summary>Dispose only after all requests have finished.</summary>
    public void Dispose() => _gate.Dispose();
}
