using SingleFlightCacheStampedeLab.Backend;
using Xunit;

namespace SingleFlightCacheStampedeLab.Tests.Backend;

public sealed class SimulatedBackendLoaderTests
{
    [Fact]
    public async Task ValuesFollowGenerationNotPhysicalAttempt()
    {
        var loader = new SimulatedBackendLoader(TimeSpan.Zero, captureEvents: true);
        BackendValue first = await loader.LoadAsync("hot", TestContext.Current.CancellationToken);
        Assert.Equal(first, await loader.LoadAsync("hot", TestContext.Current.CancellationToken));
        Assert.Equal(1, loader.AdvanceGeneration("hot"));
        Assert.Equal(BackendValue.Create("hot", 1), await loader.LoadAsync("hot", TestContext.Current.CancellationToken));
        Assert.Equal(3, loader.Snapshot().TotalLoads);
        Assert.Equal(3, loader.Snapshot().LoadsByKey["hot"]);
        Assert.Equal(new long[] { 1, 2, 3 }, loader.Events.Select(item => item.AttemptId));
        loader.ResetDiagnostics();
        Assert.Equal(0, loader.Snapshot().TotalLoads);
        Assert.Equal(1, loader.GetGeneration("hot"));
        await loader.LoadAsync("hot", TestContext.Current.CancellationToken);
        Assert.Equal(4, Assert.Single(loader.Events).AttemptId);
    }

    [Fact]
    public async Task CancellationBalancesActiveCountsAndTracksPeak()
    {
        // A timer provider holds the simulated delay without wall-clock sleeps.
        var loader = new SimulatedBackendLoader(TimeSpan.FromSeconds(1), new HeldTimerProvider());
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        Task<BackendValue> first = loader.LoadAsync("a", cancellation.Token).AsTask();
        Task<BackendValue> second = loader.LoadAsync("b", cancellation.Token).AsTask();
        Assert.Equal(2, loader.Snapshot().CurrentConcurrentLoads);
        Assert.Equal(2, loader.Snapshot().PeakConcurrentLoads);
        Assert.Throws<InvalidOperationException>(loader.ResetDiagnostics);
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => second);
        Assert.Equal(0, loader.Snapshot().CurrentConcurrentLoads);
    }

    private sealed class HeldTimerProvider : TimeProvider
    {
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
            => new HeldTimer();

        private sealed class HeldTimer : ITimer
        {
            public bool Change(TimeSpan dueTime, TimeSpan period) => true;
            public void Dispose() { }
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}
