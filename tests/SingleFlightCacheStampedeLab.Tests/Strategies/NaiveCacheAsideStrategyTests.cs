using SingleFlightCacheStampedeLab.Backend;
using SingleFlightCacheStampedeLab.Cache;
using SingleFlightCacheStampedeLab.Strategies;
using SingleFlightCacheStampedeLab.Tests.TestDoubles;
using Xunit;

namespace SingleFlightCacheStampedeLab.Tests.Strategies;

public sealed class NaiveCacheAsideStrategyTests
{
    [Fact]
    public async Task HeldColdMissesDuplicateWorkButReturnEqualValues()
    {
        var backend = new ControllableBackendLoader();
        var strategy = new NaiveCacheAsideStrategy(backend, new(), TimeSpan.FromMinutes(1));
        Task<BackendValue>[] tasks = Enumerable.Range(0, 32)
            .Select(_ => strategy.GetAsync("hot", TestContext.Current.CancellationToken).AsTask()).ToArray();
        Assert.Equal(32, backend.Snapshot().TotalLoads);
        Assert.Equal(32, backend.Snapshot().CurrentConcurrentLoads);
        backend.Release("hot");
        BackendValue[] values = await Task.WhenAll(tasks);
        Assert.All(values, value => Assert.Equal(values[0], value));
        Assert.Equal(values[0], await strategy.GetAsync("hot", TestContext.Current.CancellationToken));
        Assert.Equal(32, backend.Snapshot().TotalLoads);
        Assert.Equal(1, strategy.Snapshot().CacheHits);
    }

    [Fact]
    public async Task TtlAndExplicitExpirationReload()
    {
        var time = new ManualTimeProvider();
        var backend = new SimulatedBackendLoader(TimeSpan.Zero);
        var strategy = new NaiveCacheAsideStrategy(backend, new InMemoryCacheStore(), TimeSpan.FromSeconds(1), time);
        await strategy.GetAsync("hot", TestContext.Current.CancellationToken);
        time.Advance(TimeSpan.FromSeconds(1));
        await strategy.GetAsync("hot", TestContext.Current.CancellationToken);
        Assert.True(strategy.Expire("hot"));
        await strategy.GetAsync("hot", TestContext.Current.CancellationToken);
        Assert.Equal(3, backend.Snapshot().TotalLoads);
    }
}
