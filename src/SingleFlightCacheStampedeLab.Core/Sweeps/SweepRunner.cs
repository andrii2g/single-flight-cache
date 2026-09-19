using SingleFlightCacheStampedeLab.Backend;
using SingleFlightCacheStampedeLab.Cache;
using SingleFlightCacheStampedeLab.Metrics;
using SingleFlightCacheStampedeLab.Strategies;
using SingleFlightCacheStampedeLab.Workloads;

namespace SingleFlightCacheStampedeLab.Sweeps;

public sealed class SweepRunner
{
    public static CacheStrategyBase CreateStrategy(StrategyKind kind, SimulatedBackendLoader backend, TimeSpan ttl)
        => kind switch
        {
            StrategyKind.Naive => new NaiveCacheAsideStrategy(backend, new InMemoryCacheStore(), ttl),
            StrategyKind.GlobalLock => new GlobalLockCacheStrategy(backend, new InMemoryCacheStore(), ttl),
            StrategyKind.SingleFlight => new SingleFlightCacheStrategy(backend, new InMemoryCacheStore(), ttl),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

    public async Task<ExperimentReport> RunAsync(
        ExperimentSettings settings, Action<IReadOnlyList<RunSummary>>? reportGroup = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(settings.Profile.Iterations);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(settings.Ttl, TimeSpan.Zero);
        if (settings.Strategies.Count == 0 || settings.Profile.Scenarios.Count == 0 ||
            settings.Profile.Concurrencies.Count == 0 || settings.Profile.BackendCostsMs.Count == 0)
            throw new ArgumentException("Experiment dimensions must not be empty.", nameof(settings));

        // One small unrecorded warm-up per strategy; avoid doubling the expensive full matrix.
        foreach (StrategyKind strategy in settings.Strategies)
        {
            await RunOnceAsync(strategy, WorkloadPlanFactory.Create(ScenarioKind.MixedKeys, 8),
                1, settings.Ttl, 1, cancellationToken);
        }

        var rows = new List<RunRow>();
        var summaries = new List<RunSummary>();
        IEnumerable<ScenarioKind> scenarios = settings.Profile.Scenarios;
        if (settings.IncludeBlockingDiagnostic && !scenarios.Contains(ScenarioKind.UnrelatedKeyBlocking))
            scenarios = scenarios.Append(ScenarioKind.UnrelatedKeyBlocking);

        foreach (ScenarioKind scenario in scenarios)
        {
            IEnumerable<int> counts = scenario == ScenarioKind.UnrelatedKeyBlocking ? [2] : settings.Profile.Concurrencies;
            foreach (int concurrency in counts)
            {
                foreach (int cost in settings.Profile.BackendCostsMs)
                {
                    var group = new List<RunSummary>();
                    TimeSpan offset = TimeSpan.FromMilliseconds(Math.Min(5, cost / 10d));
                    WorkloadPlan plan = WorkloadPlanFactory.Create(scenario, concurrency, offset);
                    foreach (StrategyKind kind in settings.Strategies)
                    {
                        var metrics = new List<RunMetrics>();
                        string strategyName = "";
                        for (int iteration = 1; iteration <= settings.Profile.Iterations; iteration++)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            ScenarioResult result = await RunOnceAsync(kind, plan, cost, settings.Ttl, iteration, cancellationToken);
                            strategyName = result.Strategy;
                            RunMetrics measured = MetricsCalculator.Calculate(result);
                            metrics.Add(measured);
                            rows.Add(new(scenario, strategyName, concurrency, cost, settings.Ttl.TotalMilliseconds,
                                iteration, measured, plan.ShuffleSeed, plan.Requests.Max(request => request.DelayAfterRelease.TotalMilliseconds)));
                        }

                        group.Add(new(scenario, strategyName, concurrency, cost, settings.Ttl.TotalMilliseconds,
                            settings.Profile.Iterations, MetricsCalculator.Median(metrics), plan.ShuffleSeed,
                            plan.Requests.Max(request => request.DelayAfterRelease.TotalMilliseconds)));
                    }

                    summaries.AddRange(group);
                    reportGroup?.Invoke(group.AsReadOnly());
                }
            }
        }

        return new(DateTimeOffset.UtcNow, EnvironmentMetadata.Capture(), settings.Profile.Name,
            settings.Arguments, "nearest rank: max(0, ceil(p/100 * n) - 1)", "median of each scalar across iterations",
            settings.Strategies.Count, rows.AsReadOnly(), summaries.AsReadOnly());
    }

    private static async Task<ScenarioResult> RunOnceAsync(
        StrategyKind kind, WorkloadPlan plan, int cost, TimeSpan ttl, int iteration, CancellationToken token)
    {
        // Fresh backend, store, and strategy for every iteration, including priming and warm-up.
        var backend = new SimulatedBackendLoader(TimeSpan.FromMilliseconds(cost));
        CacheStrategyBase strategy = CreateStrategy(kind, backend, ttl);
        try
        {
            return await new WorkloadRunner().RunAsync(strategy, backend, plan, ttl, iteration, token);
        }
        finally
        {
            (strategy as IDisposable)?.Dispose();
        }
    }
}
