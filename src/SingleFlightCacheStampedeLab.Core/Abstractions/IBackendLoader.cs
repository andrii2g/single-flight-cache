using SingleFlightCacheStampedeLab.Backend;
using SingleFlightCacheStampedeLab.Diagnostics;

namespace SingleFlightCacheStampedeLab.Abstractions;

public interface IBackendLoader
{
    public ValueTask<BackendValue> LoadAsync(string key, CancellationToken cancellationToken = default);
    public BackendDiagnosticsSnapshot Snapshot();
}
