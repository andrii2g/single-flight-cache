using SingleFlightCacheStampedeLab.Diagnostics;

namespace SingleFlightCacheStampedeLab.Workloads;

public sealed record ScenarioResult(
    WorkloadPlan Plan,
    string Strategy,
    TimeSpan BackendCost,
    TimeSpan Ttl,
    int Iteration,
    TimeSpan WallTime,
    IReadOnlyList<RequestObservation> Observations,
    BackendDiagnosticsSnapshot Backend,
    StrategyDiagnosticsSnapshot Diagnostics);
