using SingleFlightCacheStampedeLab.Workloads;
using Xunit;

namespace SingleFlightCacheStampedeLab.Tests.Workloads;

public sealed class WorkloadPlanFactoryTests
{
    [Theory]
    [InlineData(100, 70, 10)]
    [InlineData(17, 14, 1)]
    [InlineData(1, 1, 0)]
    public void MixedDistributionAndShuffleAreExact(int count, int hot, int cold)
    {
        WorkloadPlan plan = WorkloadPlanFactory.Create(ScenarioKind.MixedKeys, count);
        Assert.Equal(count, plan.Requests.Count);
        Assert.Equal(hot, plan.Requests.Count(item => item.Key == "hot"));
        foreach (string key in new[] { "alpha", "beta", "gamma" })
            Assert.Equal(cold, plan.Requests.Count(item => item.Key == key));
        Assert.Equal(plan.Requests, WorkloadPlanFactory.Create(ScenarioKind.MixedKeys, count).Requests);
        Assert.Equal(Enumerable.Range(0, count), plan.Requests.Select(item => item.Index));
        Assert.Equal(1729, plan.ShuffleSeed);
    }

    [Theory]
    [InlineData(ScenarioKind.HotKeyBurst, 17, 1)]
    [InlineData(ScenarioKind.SimultaneousExpiration, 17, 1)]
    [InlineData(ScenarioKind.IndependentKeys, 17, 17)]
    [InlineData(ScenarioKind.UnrelatedKeyBlocking, 2, 2)]
    public void PlansHaveExpectedKeys(ScenarioKind kind, int count, int distinct)
    {
        WorkloadPlan plan = WorkloadPlanFactory.Create(kind, count);
        Assert.Equal(count, plan.Requests.Count);
        Assert.Equal(distinct, plan.ExpectedLogicalLoads);
        Assert.Equal(distinct, plan.Requests.Select(item => item.Key).Distinct().Count());
        Assert.Equal(kind == ScenarioKind.SimultaneousExpiration, plan.PrimeAndExpireHot);
    }
}
