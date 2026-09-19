using SingleFlightCacheStampedeLab.Workloads;

namespace SingleFlightCacheStampedeLab.Sweeps;

public enum StrategyKind { Naive, GlobalLock, SingleFlight }

public sealed record SweepProfile(
    string Name,
    IReadOnlyList<ScenarioKind> Scenarios,
    IReadOnlyList<int> Concurrencies,
    IReadOnlyList<int> BackendCostsMs,
    int Iterations)
{
    public static SweepProfile Quick => new("quick",
        Array.AsReadOnly(new[] { ScenarioKind.HotKeyBurst, ScenarioKind.IndependentKeys, ScenarioKind.MixedKeys }),
        Array.AsReadOnly(new[] { 1, 8, 32, 128, 256 }), Array.AsReadOnly(new[] { 5, 20 }), 3);

    public static SweepProfile Full => new("full",
        Array.AsReadOnly(new[] { ScenarioKind.HotKeyBurst, ScenarioKind.SimultaneousExpiration,
            ScenarioKind.MixedKeys, ScenarioKind.IndependentKeys, ScenarioKind.UnrelatedKeyBlocking }),
        Array.AsReadOnly(new[] { 1, 2, 4, 8, 16, 32, 64, 128, 256, 512 }),
        Array.AsReadOnly(new[] { 1, 5, 20, 100 }), 5);
}

public sealed record ExperimentSettings(
    SweepProfile Profile,
    IReadOnlyList<StrategyKind> Strategies,
    TimeSpan Ttl,
    IReadOnlyList<string> Arguments,
    bool IncludeBlockingDiagnostic = true);
