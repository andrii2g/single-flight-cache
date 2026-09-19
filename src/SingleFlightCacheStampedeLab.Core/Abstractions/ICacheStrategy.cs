using SingleFlightCacheStampedeLab.Backend;
using SingleFlightCacheStampedeLab.Diagnostics;

namespace SingleFlightCacheStampedeLab.Abstractions;

public interface ICacheStrategy
{
    public string Name { get; }
    public ValueTask<BackendValue> GetAsync(string key, CancellationToken cancellationToken = default);
    public bool Expire(string key);
    public void Clear();
    public StrategyDiagnosticsSnapshot Snapshot();
}
