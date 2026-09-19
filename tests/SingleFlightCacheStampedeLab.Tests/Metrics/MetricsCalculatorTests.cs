using SingleFlightCacheStampedeLab.Backend;
using SingleFlightCacheStampedeLab.Diagnostics;
using SingleFlightCacheStampedeLab.Metrics;
using SingleFlightCacheStampedeLab.Workloads;
using Xunit;

namespace SingleFlightCacheStampedeLab.Tests.Metrics;

public sealed class MetricsCalculatorTests
{
    [Fact]
    public void ExactFormulasAndLatencyRanks()
    {
        var result = new ScenarioResult(WorkloadPlanFactory.Create(ScenarioKind.HotKeyBurst, 4),
            "test", TimeSpan.Zero, TimeSpan.FromSeconds(1), 1, TimeSpan.FromSeconds(2),
            Enumerable.Range(1, 4).Select(i => new RequestObservation(i - 1, "hot",
                BackendValue.Create("hot", 0), TimeSpan.FromMilliseconds(i))).ToArray(),
            new BackendDiagnosticsSnapshot(4, 0, 4, new Dictionary<string, long> { ["hot"] = 4 }),
            new StrategyDiagnosticsSnapshot(CacheMisses: 4, CoalescedCallers: 3));
        RunMetrics metrics = MetricsCalculator.Calculate(result);
        Assert.Equal(3, metrics.DuplicateLoads);
        Assert.Equal(4, metrics.Amplification);
        Assert.Equal(2, metrics.Throughput);
        Assert.Equal(0.75, metrics.CoalescingRatio);
        Assert.Equal(2, metrics.P50Ms);
        Assert.Equal(4, metrics.P95Ms);
        Assert.Equal(4, metrics.P99Ms);
        Assert.Equal(4, metrics.MaxMs);
        Assert.Equal(metrics, MetricsCalculator.Median([metrics, metrics]));
        RunMetrics clamped = MetricsCalculator.Calculate(result with
        {
            Backend = result.Backend with { TotalLoads = 0 },
            Plan = result.Plan with { ExpectedLogicalLoads = 0 }
        });
        Assert.Equal(0, clamped.DuplicateLoads);
        Assert.Equal(0, clamped.Amplification);
    }
}
