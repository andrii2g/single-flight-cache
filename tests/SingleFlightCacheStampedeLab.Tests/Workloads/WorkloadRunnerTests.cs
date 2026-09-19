using SingleFlightCacheStampedeLab.Backend;
using SingleFlightCacheStampedeLab.Strategies;
using SingleFlightCacheStampedeLab.Workloads;
using Xunit;

namespace SingleFlightCacheStampedeLab.Tests.Workloads;

public sealed class WorkloadRunnerTests
{
    [Theory]
    [InlineData(ScenarioKind.HotKeyBurst, 0)]
    [InlineData(ScenarioKind.SimultaneousExpiration, 1)]
    public async Task BurstValidatesValuesAndExcludesPriming(ScenarioKind kind, long expectedGeneration)
    {
        var backend = new SimulatedBackendLoader(TimeSpan.Zero);
        var ttl = TimeSpan.FromMinutes(1);
        var strategy = new SingleFlightCacheStrategy(backend, new(), ttl);
        ScenarioResult result = await new WorkloadRunner().RunAsync(strategy, backend,
            WorkloadPlanFactory.Create(kind, 256), ttl, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(1, result.Backend.TotalLoads);
        Assert.Equal(256, result.Diagnostics.CacheHits + result.Diagnostics.CacheMisses);
        Assert.All(result.Observations, item => Assert.Equal(BackendValue.Create("hot", expectedGeneration), item.Value));
    }

    [Fact]
    public void SemanticValidationRejectsWrongKeyGenerationPayloadAndFailures()
    {
        var expected = new Dictionary<string, long> { ["hot"] = 1 };
        foreach (BackendValue value in new[] { BackendValue.Create("wrong", 1), BackendValue.Create("hot", 0), new("hot", 1, "wrong") })
        {
            Assert.Throws<LabValidationException>(() => WorkloadRunner.Validate(
                [new(0, "hot", value, TimeSpan.Zero)], expected));
        }

        Assert.Throws<LabValidationException>(() => WorkloadRunner.Validate(
            [new(0, "hot", null, TimeSpan.Zero, Failure: "controlled failure")], expected));
    }

    [Fact]
    public async Task CanceledRunPropagatesCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var backend = new SimulatedBackendLoader(TimeSpan.Zero);
        var ttl = TimeSpan.FromSeconds(1);
        var strategy = new SingleFlightCacheStrategy(backend, new(), ttl);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new WorkloadRunner().RunAsync(
            strategy, backend, WorkloadPlanFactory.Create(ScenarioKind.HotKeyBurst, 16), ttl,
            cancellationToken: cancellation.Token));
    }
}
