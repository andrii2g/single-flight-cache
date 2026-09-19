namespace SingleFlightCacheStampedeLab.Workloads;

public sealed class BurstGate : IDisposable
{
    private readonly int _expected;
    private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly CancellationTokenRegistration _registration;
    private int _arrived;

    public BurstGate(int expectedParticipants, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(expectedParticipants);
        _expected = expectedParticipants;
        _registration = cancellationToken.Register(() => Cancel(cancellationToken));
    }

    public Task Ready => _ready.Task;
    public int ReadyCount => Volatile.Read(ref _arrived);

    public async Task ArriveAndWaitAsync(CancellationToken cancellationToken = default)
    {
        using CancellationTokenRegistration registration = cancellationToken.Register(() => Cancel(cancellationToken));
        int arrived = Interlocked.Increment(ref _arrived);
        if (arrived > _expected) throw new InvalidOperationException("More callers arrived than the burst expects.");
        if (arrived == _expected) _ready.TrySetResult();
        await _release.Task;
    }

    public void Release()
    {
        if (!_ready.Task.IsCompletedSuccessfully)
            throw new InvalidOperationException("Cannot release a burst before all callers are ready.");
        _release.TrySetResult();
    }

    private void Cancel(CancellationToken token)
    {
        // A canceled participant aborts the whole burst, including a coordinator awaiting Ready.
        _ready.TrySetCanceled(token);
        _release.TrySetCanceled(token);
    }

    public void Dispose() => _registration.Dispose();
}
