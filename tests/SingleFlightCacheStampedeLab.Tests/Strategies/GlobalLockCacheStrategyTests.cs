using SingleFlightCacheStampedeLab.Backend;
using SingleFlightCacheStampedeLab.Strategies;
using SingleFlightCacheStampedeLab.Tests.TestDoubles;
using Xunit;

namespace SingleFlightCacheStampedeLab.Tests.Strategies;

public sealed class GlobalLockCacheStrategyTests
{
    [Fact]
    public async Task SameKeyIsDoubleCheckedAfterLockAcquisition()
    {
        var backend = new ControllableBackendLoader();
        using var strategy = new GlobalLockCacheStrategy(backend, new(), TimeSpan.FromMinutes(1));
        Task<BackendValue>[] tasks = Enumerable.Range(0, 64)
            .Select(_ => strategy.GetAsync("hot", TestContext.Current.CancellationToken).AsTask()).ToArray();
        Assert.Equal(1, backend.Snapshot().TotalLoads);
        Assert.Equal(63, strategy.Snapshot().ContendedLockAcquisitions);
        backend.Release("hot");
        BackendValue[] values = await Task.WhenAll(tasks);
        Assert.All(values, value => Assert.Equal(values[0], value));
        Assert.Equal(1, backend.Snapshot().TotalLoads);
        Assert.Equal(64, strategy.Snapshot().LockAcquisitions);
    }

    [Fact]
    public async Task UnrelatedKeysSerializeButCacheHitsBypassGate()
    {
        var backend = new ControllableBackendLoader();
        using var strategy = new GlobalLockCacheStrategy(backend, new(), TimeSpan.FromMinutes(1));
        backend.Release("cached");
        await strategy.GetAsync("cached", TestContext.Current.CancellationToken);
        Task<BackendValue> first = strategy.GetAsync("a", TestContext.Current.CancellationToken).AsTask();
        await backend.WaitUntilEnteredAsync("a", TestContext.Current.CancellationToken);
        Task<BackendValue> second = strategy.GetAsync("b", TestContext.Current.CancellationToken).AsTask();
        Assert.False(backend.For("b").Entered.IsOpen);
        Assert.Equal(1, strategy.Snapshot().UnrelatedKeyContentions);
        Assert.True(strategy.GetAsync("cached", TestContext.Current.CancellationToken).IsCompletedSuccessfully);
        backend.Release("a");
        await first;
        await backend.WaitUntilEnteredAsync("b", TestContext.Current.CancellationToken);
        backend.Release("b");
        await second;
        Assert.Equal(1, backend.Snapshot().PeakConcurrentLoads);
    }

    [Fact]
    public async Task FailureReleasesGateAndAllowsRetry()
    {
        var backend = new ControllableBackendLoader();
        backend.For("a").Failure = new InvalidOperationException("backend failed");
        using var strategy = new GlobalLockCacheStrategy(backend, new(), TimeSpan.FromMinutes(1));
        Task<BackendValue> failed = strategy.GetAsync("a", TestContext.Current.CancellationToken).AsTask();
        backend.Release("a");
        await Assert.ThrowsAsync<InvalidOperationException>(() => failed);
        backend.For("a").Failure = null;
        Assert.Equal("a", (await strategy.GetAsync("a", TestContext.Current.CancellationToken)).Key);
        Assert.Equal(2, backend.Snapshot().TotalLoads);
    }

    [Fact]
    public async Task CanceledQueuedCallerDoesNotReleaseAnotherCallersGate()
    {
        var backend = new ControllableBackendLoader();
        using var strategy = new GlobalLockCacheStrategy(backend, new(), TimeSpan.FromMinutes(1));
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        Task<BackendValue> first = strategy.GetAsync("a", TestContext.Current.CancellationToken).AsTask();
        Task<BackendValue> second = strategy.GetAsync("b", cancellation.Token).AsTask();
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => second);
        Assert.False(backend.For("b").Entered.IsOpen);
        backend.Release("a");
        await first;
        Assert.Equal(1, strategy.Snapshot().LockAcquisitions);
    }
}
