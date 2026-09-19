using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using SingleFlightCacheStampedeLab.Abstractions;
using SingleFlightCacheStampedeLab.Backend;
using SingleFlightCacheStampedeLab.Diagnostics;

namespace SingleFlightCacheStampedeLab.Tests.TestDoubles;

internal sealed class ControllableBackendLoader : IBackendLoader
{
    private readonly ConcurrentDictionary<string, Control> _controls = new();
    private readonly ConcurrentDictionary<string, long> _counts = new();
    private long _total;
    private int _current, _peak;

    public Control For(string key) => _controls.GetOrAdd(key, static _ => new());
    public Task WaitUntilEnteredAsync(string key, CancellationToken token) => For(key).Entered.WaitAsync(token);
    public void Release(string key) => For(key).Release.Open();
    public void BeginNewWave(string key, long generation) => _controls[key] = new() { Generation = generation };

    public async ValueTask<BackendValue> LoadAsync(string key, CancellationToken cancellationToken = default)
    {
        Control control = For(key);
        long generation = control.Generation;
        Exception? failure = control.Failure;
        control.LastToken = cancellationToken;
        Interlocked.Increment(ref _total);
        _counts.AddOrUpdate(key, 1, static (_, count) => count + 1);
        int current = Interlocked.Increment(ref _current);
        int peak;
        while (current > (peak = Volatile.Read(ref _peak)) &&
               Interlocked.CompareExchange(ref _peak, current, peak) != peak) { }
        control.Entered.Open();
        try
        {
            await control.Release.WaitAsync(cancellationToken);
            if (failure is not null) throw failure;
            return BackendValue.Create(key, generation);
        }
        finally
        {
            Interlocked.Decrement(ref _current);
        }
    }

    public BackendDiagnosticsSnapshot Snapshot() => new(
        Interlocked.Read(ref _total), Volatile.Read(ref _current), Volatile.Read(ref _peak),
        new ReadOnlyDictionary<string, long>(new Dictionary<string, long>(_counts)));

    internal sealed class Control
    {
        public AsyncTestGate Entered { get; } = new();
        public AsyncTestGate Release { get; } = new();
        public long Generation { get; set; }
        public Exception? Failure { get; set; }
        public CancellationToken LastToken { get; set; }
    }
}
