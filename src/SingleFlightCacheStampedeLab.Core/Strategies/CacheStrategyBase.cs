using System.Diagnostics.CodeAnalysis;
using SingleFlightCacheStampedeLab.Abstractions;
using SingleFlightCacheStampedeLab.Backend;
using SingleFlightCacheStampedeLab.Cache;
using SingleFlightCacheStampedeLab.Diagnostics;

namespace SingleFlightCacheStampedeLab.Strategies;

public abstract class CacheStrategyBase : ICacheStrategy
{
    private StrategyDiagnostics _diagnostics = new();

    protected CacheStrategyBase(IBackendLoader backend, InMemoryCacheStore store, TimeSpan ttl, TimeProvider? timeProvider)
    {
        ArgumentNullException.ThrowIfNull(backend);
        ArgumentNullException.ThrowIfNull(store);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(ttl, TimeSpan.Zero);
        Backend = backend;
        Store = store;
        Ttl = ttl;
        TimeProvider = timeProvider ?? TimeProvider.System;
    }

    protected IBackendLoader Backend { get; }
    protected InMemoryCacheStore Store { get; }
    protected TimeSpan Ttl { get; }
    protected TimeProvider TimeProvider { get; }
    private protected StrategyDiagnostics Diagnostics => _diagnostics;

    public abstract string Name { get; }
    public abstract ValueTask<BackendValue> GetAsync(string key, CancellationToken cancellationToken = default);
    public bool Expire(string key) => Store.Expire(key);
    public void Clear() => Store.Clear();
    public StrategyDiagnosticsSnapshot Snapshot() => Diagnostics.Snapshot();

    /// <summary>Call only when no requests or shared backend operations are active.</summary>
    public void ResetDiagnostics() => _diagnostics = new();

    protected bool TryGetFresh(string key, [NotNullWhen(true)] out BackendValue? value)
        => Store.TryGetFresh(key, TimeProvider.GetUtcNow(), out value);

    protected void Cache(string key, BackendValue value) => Store.Set(key, value, TimeProvider.GetUtcNow() + Ttl);

    protected static void ValidateRequest(string key, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        cancellationToken.ThrowIfCancellationRequested();
    }
}
