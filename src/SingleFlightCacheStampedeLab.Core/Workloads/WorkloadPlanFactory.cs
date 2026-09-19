using System.Globalization;
using SingleFlightCacheStampedeLab.Utilities;

namespace SingleFlightCacheStampedeLab.Workloads;

public static class WorkloadPlanFactory
{
    public static WorkloadPlan Create(ScenarioKind scenario, int concurrency, TimeSpan waveOffset = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(concurrency);
        ArgumentOutOfRangeException.ThrowIfLessThan(waveOffset, TimeSpan.Zero);
        if (!Enum.IsDefined(scenario)) throw new ArgumentOutOfRangeException(nameof(scenario));
        if (scenario == ScenarioKind.UnrelatedKeyBlocking && concurrency != 2)
            throw new ArgumentException("UnrelatedKeyBlocking requires exactly two callers.", nameof(concurrency));

        string[] keys = scenario switch
        {
            ScenarioKind.HotKeyBurst or ScenarioKind.SimultaneousExpiration => Enumerable.Repeat("hot", concurrency).ToArray(),
            ScenarioKind.IndependentKeys => Enumerable.Range(1, concurrency)
                .Select(index => "key-" + index.ToString("D4", CultureInfo.InvariantCulture)).ToArray(),
            ScenarioKind.UnrelatedKeyBlocking => ["slow-A", "fast-B"],
            ScenarioKind.MixedKeys => MixedKeys(concurrency),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };
        WorkloadRequest[] requests = keys.Select((key, index) => new WorkloadRequest(index, key,
            scenario == ScenarioKind.UnrelatedKeyBlocking && index == 1 ? waveOffset : TimeSpan.Zero)).ToArray();
        string description = scenario switch
        {
            ScenarioKind.HotKeyBurst => "Synchronized cold requests for one hot key.",
            ScenarioKind.SimultaneousExpiration => "Prime hot, advance its generation, explicitly expire, then release a burst.",
            ScenarioKind.MixedKeys => "70/10/10/10 key mix; integer remainder belongs to hot; shuffle seed 1729.",
            ScenarioKind.IndependentKeys => "Each caller requests a distinct cold key.",
            _ => "Start slow-A, then fast-B after the configured wave offset; equal simulated backend cost."
        };
        return new(scenario, Array.AsReadOnly(requests), concurrency, keys.Distinct(StringComparer.Ordinal).Count(),
            scenario == ScenarioKind.SimultaneousExpiration, description,
            scenario == ScenarioKind.MixedKeys ? DeterministicShuffle.Seed : null);
    }

    private static string[] MixedKeys(int concurrency)
    {
        int cold = concurrency / 10;
        string[] keys = Enumerable.Repeat("hot", concurrency - 3 * cold)
            .Concat(Enumerable.Repeat("alpha", cold))
            .Concat(Enumerable.Repeat("beta", cold))
            .Concat(Enumerable.Repeat("gamma", cold)).ToArray();
        DeterministicShuffle.Shuffle(keys);
        return keys;
    }
}
