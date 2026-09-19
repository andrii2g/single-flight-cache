namespace SingleFlightCacheStampedeLab.Workloads;

public sealed record WorkloadPlan(
    ScenarioKind Scenario,
    IReadOnlyList<WorkloadRequest> Requests,
    int Concurrency,
    int ExpectedLogicalLoads,
    bool PrimeAndExpireHot,
    string Description,
    int? ShuffleSeed = null);
