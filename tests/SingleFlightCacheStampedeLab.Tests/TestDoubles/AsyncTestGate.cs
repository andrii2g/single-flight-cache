namespace SingleFlightCacheStampedeLab.Tests.TestDoubles;

internal sealed class AsyncTestGate
{
    private readonly TaskCompletionSource _source = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public bool IsOpen => _source.Task.IsCompletedSuccessfully;
    public Task WaitAsync(CancellationToken token = default) => _source.Task.WaitAsync(token);
    public void Open() => _source.TrySetResult();
}
