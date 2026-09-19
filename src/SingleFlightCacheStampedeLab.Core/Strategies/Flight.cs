namespace SingleFlightCacheStampedeLab.Strategies;

internal sealed class Flight<T>
{
    private readonly TaskCompletionSource<T> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _waiters;

    public Task<T> Task => _completion.Task;
    public int Waiters => Volatile.Read(ref _waiters);
    public void AddWaiter() => Interlocked.Increment(ref _waiters);
    public void RemoveWaiter() => Interlocked.Decrement(ref _waiters);
    public void Complete(T value) => _completion.TrySetResult(value);

    public void Fail(Exception exception)
    {
        _completion.TrySetException(exception);
        // Observe the failure even if every caller abandoned its wait.
        _ = Task.Exception;
    }
}
