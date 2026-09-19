using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using SingleFlightCacheStampedeLab.Abstractions;
using SingleFlightCacheStampedeLab.Diagnostics;
using SingleFlightCacheStampedeLab.Utilities;

namespace SingleFlightCacheStampedeLab.Backend;

public sealed class SimulatedBackendLoader : IBackendLoader
{
    private readonly TimeProvider _timeProvider;
    private readonly ConcurrentDictionary<string, long> _generations = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, long> _loadsByKey = new(StringComparer.Ordinal);
    private readonly ConcurrentQueue<BackendLoadEvent> _events = new();
    private readonly bool _captureEvents;
    private long _totalLoads;
    private long _attemptId;
    private int _currentLoads;
    private int _peakLoads;

    public SimulatedBackendLoader(TimeSpan cost, TimeProvider? timeProvider = null, bool captureEvents = false)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(cost, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(cost.TotalMilliseconds, uint.MaxValue - 1d);
        Cost = cost;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _captureEvents = captureEvents;
    }

    public TimeSpan Cost { get; }
    public IReadOnlyList<BackendLoadEvent> Events => Array.AsReadOnly(_events.ToArray());

    public async ValueTask<BackendValue> LoadAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        cancellationToken.ThrowIfCancellationRequested();
        long generation = GetGeneration(key);
        long attempt = Interlocked.Increment(ref _attemptId);
        int current = Interlocked.Increment(ref _currentLoads);
        int peak;
        while (current > (peak = Volatile.Read(ref _peakLoads)) &&
               Interlocked.CompareExchange(ref _peakLoads, current, peak) != peak)
        {
        }

        Interlocked.Increment(ref _totalLoads);
        _loadsByKey.AddOrUpdate(key, 1, static (_, count) => count + 1);
        DateTimeOffset startedAt = _timeProvider.GetUtcNow();
        ValueStopwatch timer = ValueStopwatch.StartNew();
        try
        {
            await Task.Delay(Cost, _timeProvider, cancellationToken);
            return BackendValue.Create(key, generation);
        }
        finally
        {
            if (_captureEvents)
            {
                _events.Enqueue(new(attempt, key, generation, startedAt, timer.Elapsed));
            }

            Interlocked.Decrement(ref _currentLoads);
        }
    }

    public long AdvanceGeneration(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return _generations.AddOrUpdate(key, 1, static (_, generation) => checked(generation + 1));
    }

    public long GetGeneration(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return _generations.GetValueOrDefault(key);
    }

    // The experiment coordinator calls reset only when all operations have finished.
    public void ResetDiagnostics()
    {
        if (Volatile.Read(ref _currentLoads) != 0)
        {
            throw new InvalidOperationException("Cannot reset backend diagnostics while loads are active.");
        }

        Interlocked.Exchange(ref _totalLoads, 0);
        Interlocked.Exchange(ref _peakLoads, 0);
        _loadsByKey.Clear();
        _events.Clear();
        // Attempt IDs remain monotonic across runs.
    }

    public BackendDiagnosticsSnapshot Snapshot() => new(
        Interlocked.Read(ref _totalLoads),
        Volatile.Read(ref _currentLoads),
        Volatile.Read(ref _peakLoads),
        new ReadOnlyDictionary<string, long>(new Dictionary<string, long>(_loadsByKey, StringComparer.Ordinal)));
}
