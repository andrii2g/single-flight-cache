using SingleFlightCacheStampedeLab.Backend;
using SingleFlightCacheStampedeLab.Strategies;
using SingleFlightCacheStampedeLab.Tests.TestDoubles;
using Xunit;

namespace SingleFlightCacheStampedeLab.Tests.Strategies;

public sealed class SingleFlightCacheStrategyTests
{
    [Fact]
    public async Task All256ColdCallersJoinOneFlight()
    {
        var backend = new ControllableBackendLoader();
        var strategy = new SingleFlightCacheStrategy(backend, new(), TimeSpan.FromMinutes(1));
        var start = new AsyncTestGate();
        var issued = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int count = 0;
        async Task<BackendValue> Request()
        {
            await start.WaitAsync(TestContext.Current.CancellationToken);
            ValueTask<BackendValue> request = strategy.GetAsync("hot", TestContext.Current.CancellationToken);
            if (Interlocked.Increment(ref count) == 256) issued.SetResult();
            return await request;
        }

        Task<BackendValue>[] tasks = Enumerable.Range(0, 256).Select(_ => Request()).ToArray();
        start.Open();
        await issued.Task.WaitAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, backend.Snapshot().TotalLoads);
        Assert.Equal(255, strategy.Snapshot().CoalescedCallers);
        Assert.Equal(255, strategy.Snapshot().CurrentFlightWaiters);
        backend.Release("hot");
        BackendValue[] values = await Task.WhenAll(tasks);
        Assert.All(values, value => Assert.Equal(values[0], value));
        Assert.Equal(0, strategy.Snapshot().CurrentFlightWaiters);
        Assert.Equal(255, strategy.Snapshot().PeakFlightWaiters);
        await strategy.GetAsync("hot", TestContext.Current.CancellationToken);
        Assert.Equal(1, strategy.Snapshot().FlightsCreated);
        Assert.Equal(1, strategy.Snapshot().CacheHits);
    }

    [Fact]
    public async Task DifferentKeysEnterBackendConcurrently()
    {
        var backend = new ControllableBackendLoader();
        var strategy = new SingleFlightCacheStrategy(backend, new(), TimeSpan.FromMinutes(1));
        Task<BackendValue> first = strategy.GetAsync("a", TestContext.Current.CancellationToken).AsTask();
        await backend.WaitUntilEnteredAsync("a", TestContext.Current.CancellationToken);
        Task<BackendValue> second = strategy.GetAsync("b", TestContext.Current.CancellationToken).AsTask();
        await backend.WaitUntilEnteredAsync("b", TestContext.Current.CancellationToken);
        Assert.Equal(2, backend.Snapshot().CurrentConcurrentLoads);
        backend.Release("a");
        backend.Release("b");
        await Task.WhenAll(first, second);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancellationOnlyAbandonsThatCaller(bool cancelLeader)
    {
        var backend = new ControllableBackendLoader();
        var strategy = new SingleFlightCacheStrategy(backend, new(), TimeSpan.FromMinutes(1));
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        Task<BackendValue> leader = strategy.GetAsync("hot",
            cancelLeader ? cancellation.Token : TestContext.Current.CancellationToken).AsTask();
        Task<BackendValue> waiter = strategy.GetAsync("hot",
            cancelLeader ? TestContext.Current.CancellationToken : cancellation.Token).AsTask();
        Task<BackendValue> survivor = strategy.GetAsync("hot", TestContext.Current.CancellationToken).AsTask();
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelLeader ? leader : waiter);
        Assert.False(backend.For("hot").LastToken.CanBeCanceled);
        Assert.Equal(1, backend.Snapshot().CurrentConcurrentLoads);
        backend.Release("hot");
        BackendValue value = await survivor;
        Assert.Equal(value, await (cancelLeader ? waiter : leader));
        Assert.Equal(value, await strategy.GetAsync("hot", TestContext.Current.CancellationToken));
        Assert.Equal(1, backend.Snapshot().TotalLoads);
    }

    [Fact]
    public async Task FailureIsSharedRemovedAndRetried()
    {
        var backend = new ControllableBackendLoader();
        var failure = new InvalidOperationException("controlled failure");
        backend.For("hot").Failure = failure;
        var strategy = new SingleFlightCacheStrategy(backend, new(), TimeSpan.FromMinutes(1));
        Task<BackendValue> first = strategy.GetAsync("hot", TestContext.Current.CancellationToken).AsTask();
        Task<BackendValue> second = strategy.GetAsync("hot", TestContext.Current.CancellationToken).AsTask();
        backend.Release("hot");
        Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => first));
        Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => second));
        backend.For("hot").Failure = null;
        Assert.Equal("hot", (await strategy.GetAsync("hot", TestContext.Current.CancellationToken)).Key);
        Assert.Equal(2, backend.Snapshot().TotalLoads);
        Assert.Equal(2, strategy.Snapshot().FlightsCreated);
    }

    [Fact]
    public async Task SuccessfulFlightIsRemovedAndExpirationLoadsNewGeneration()
    {
        var backend = new SimulatedBackendLoader(TimeSpan.Zero);
        var strategy = new SingleFlightCacheStrategy(backend, new(), TimeSpan.FromMinutes(1));
        await strategy.GetAsync("hot", TestContext.Current.CancellationToken);
        Assert.Equal(0, strategy.ActiveFlights);
        backend.AdvanceGeneration("hot");
        strategy.Expire("hot");
        Assert.Equal(1, (await strategy.GetAsync("hot", TestContext.Current.CancellationToken)).Generation);
        Assert.Equal(2, backend.Snapshot().TotalLoads);
        Assert.Equal(0, strategy.ActiveFlights);
    }

    [Fact]
    public async Task SimultaneousExpirationCoalescesTheNewGeneration()
    {
        var backend = new ControllableBackendLoader();
        var strategy = new SingleFlightCacheStrategy(backend, new(), TimeSpan.FromMinutes(1));
        backend.Release("hot");
        Assert.Equal(0, (await strategy.GetAsync("hot", TestContext.Current.CancellationToken)).Generation);
        backend.BeginNewWave("hot", 1);
        Assert.True(strategy.Expire("hot"));
        strategy.ResetDiagnostics();
        Task<BackendValue>[] tasks = Enumerable.Range(0, 256)
            .Select(_ => strategy.GetAsync("hot", TestContext.Current.CancellationToken).AsTask()).ToArray();
        Assert.Equal(2, backend.Snapshot().TotalLoads); // One prime, one new physical load.
        Assert.Equal(255, strategy.Snapshot().CoalescedCallers);
        backend.Release("hot");
        BackendValue[] values = await Task.WhenAll(tasks);
        Assert.All(values, value => Assert.Equal(BackendValue.Create("hot", 1), value));
    }
    [Fact]
    public void OldCleanupCannotRemoveNewReplacementFlight()
    {
        var registry = new FlightRegistry<BackendValue>();
        var first = new Flight<BackendValue>();
        Assert.Same(first, registry.JoinOrCreate("hot", first));
        first.Complete(BackendValue.Create("hot", 0));
        // Pause F1's finally cleanup; a retry retires F1 and installs F2.
        var second = new Flight<BackendValue>();
        Assert.Same(second, registry.JoinOrCreate("hot", second));
        Assert.False(registry.RemoveExact("hot", first));
        Assert.Same(second, registry.JoinOrCreate("hot", new()));
        Assert.Equal(1, registry.Count);
        Assert.True(registry.RemoveExact("hot", second));
    }
}
