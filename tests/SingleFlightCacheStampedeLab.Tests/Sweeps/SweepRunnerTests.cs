using SingleFlightCacheStampedeLab.Sweeps;
using SingleFlightCacheStampedeLab.Workloads;
using Xunit;

namespace SingleFlightCacheStampedeLab.Tests.Sweeps;

public sealed class SweepRunnerTests
{
    [Fact]
    public async Task RunsIsolatedIterationsInDeterministicOrderAndExcludesWarmup()
    {
        var settings = new ExperimentSettings(new("test", [ScenarioKind.SimultaneousExpiration], [4], [0], 3),
            [StrategyKind.GlobalLock, StrategyKind.SingleFlight], TimeSpan.FromSeconds(1), [], false);
        ExperimentReport report = await new SweepRunner().RunAsync(settings, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(2, report.WarmupRuns);
        Assert.Equal(6, report.Runs.Count);
        Assert.Equal(2, report.Summaries.Count);
        Assert.Equal(new[] { 1, 2, 3, 1, 2, 3 }, report.Runs.Select(row => row.Iteration));
        Assert.All(report.Runs, row =>
        {
            Assert.Equal(1, row.Metrics.ActualLoads);
            Assert.Equal(4, row.Metrics.RequestCount);
            Assert.Equal(4, row.Metrics.CacheHits + row.Metrics.CacheMisses);
        });
        Assert.All(report.Summaries, row => Assert.Equal(1, row.Median.ActualLoads));
    }

    [Fact]
    public void ProfilesKeepContractDefaults()
    {
        Assert.Equal(new[] { 1, 8, 32, 128, 256 }, SweepProfile.Quick.Concurrencies);
        Assert.Equal(new[] { 5, 20 }, SweepProfile.Quick.BackendCostsMs);
        Assert.Equal(3, SweepProfile.Quick.Iterations);
        Assert.Equal(new[] { 1, 2, 4, 8, 16, 32, 64, 128, 256, 512 }, SweepProfile.Full.Concurrencies);
        Assert.Equal(new[] { 1, 5, 20, 100 }, SweepProfile.Full.BackendCostsMs);
        Assert.Equal(5, SweepProfile.Full.Iterations);
    }
}
