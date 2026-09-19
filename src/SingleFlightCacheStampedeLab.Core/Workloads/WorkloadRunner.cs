using SingleFlightCacheStampedeLab.Backend;
using SingleFlightCacheStampedeLab.Strategies;
using SingleFlightCacheStampedeLab.Utilities;

namespace SingleFlightCacheStampedeLab.Workloads;

public sealed class WorkloadRunner
{
    public async Task<ScenarioResult> RunAsync(
        CacheStrategyBase strategy, SimulatedBackendLoader backend, WorkloadPlan plan,
        TimeSpan ttl, int iteration = 1, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(strategy);
        ArgumentNullException.ThrowIfNull(backend);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(iteration);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(ttl, TimeSpan.Zero);
        if (plan.Requests.Count != plan.Concurrency || plan.Concurrency <= 0)
            throw new ArgumentException("Plan request count must equal its positive concurrency.", nameof(plan));
        cancellationToken.ThrowIfCancellationRequested();

        strategy.Clear();
        if (plan.PrimeAndExpireHot)
        {
            await strategy.GetAsync("hot", cancellationToken);
            backend.AdvanceGeneration("hot");
            strategy.Expire("hot");
        }

        // Priming is setup, not measured backend work.
        strategy.ResetDiagnostics();
        backend.ResetDiagnostics();
        Dictionary<string, long> expected = plan.Requests.Select(request => request.Key)
            .Distinct(StringComparer.Ordinal).ToDictionary(key => key, backend.GetGeneration, StringComparer.Ordinal);

        RequestObservation[] observations;
        TimeSpan elapsed;
        if (plan.Scenario == ScenarioKind.UnrelatedKeyBlocking)
        {
            ValueStopwatch timer = ValueStopwatch.StartNew();
            // Calling A starts its backend before the first await; then B observes the configured offset.
            Task<RequestObservation>[] tasks = plan.Requests.Select(request =>
                ObserveAsync(strategy, request, null, cancellationToken)).ToArray();
            observations = await Task.WhenAll(tasks);
            elapsed = timer.Elapsed;
        }
        else
        {
            using var gate = new BurstGate(plan.Concurrency, cancellationToken);
            Task<RequestObservation>[] tasks = plan.Requests.Select(request =>
                ObserveAsync(strategy, request, gate, cancellationToken)).ToArray();
            try
            {
                await gate.Ready.WaitAsync(cancellationToken);
                ValueStopwatch timer = ValueStopwatch.StartNew();
                gate.Release();
                observations = await Task.WhenAll(tasks);
                elapsed = timer.Elapsed;
            }
            catch (OperationCanceledException)
            {
                // Observe all participants even if cancellation interrupted the ready phase.
                await Task.WhenAll(tasks);
                throw;
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        Validate(observations, expected);
        return new(plan, strategy.Name, backend.Cost, ttl, iteration, elapsed,
            Array.AsReadOnly(observations), backend.Snapshot(), strategy.Snapshot());
    }

    private static async Task<RequestObservation> ObserveAsync(
        CacheStrategyBase strategy, WorkloadRequest request, BurstGate? gate, CancellationToken token)
    {
        ValueStopwatch timer = ValueStopwatch.StartNew();
        try
        {
            if (gate is not null) await gate.ArriveAndWaitAsync(token);
            if (request.DelayAfterRelease > TimeSpan.Zero) await Task.Delay(request.DelayAfterRelease, token);
            timer = ValueStopwatch.StartNew();
            BackendValue value = await strategy.GetAsync(request.Key, token);
            return new(request.Index, request.Key, value, timer.Elapsed);
        }
        catch (OperationCanceledException exception)
        {
            return new(request.Index, request.Key, null, timer.Elapsed, true, exception.Message);
        }
        catch (Exception exception)
        {
            return new(request.Index, request.Key, null, timer.Elapsed, false,
                $"{exception.GetType().Name}: {exception.Message}");
        }
    }

    internal static void Validate(IReadOnlyList<RequestObservation> observations, IReadOnlyDictionary<string, long> expected)
    {
        foreach (RequestObservation observation in observations)
        {
            if (!observation.Succeeded)
                throw new LabValidationException($"Request {observation.Index} for '{observation.Key}' failed: {observation.Failure}");
            BackendValue value = observation.Value!;
            BackendValue required = BackendValue.Create(observation.Key, expected[observation.Key]);
            if (value != required)
                throw new LabValidationException(
                    $"Request {observation.Index} for '{observation.Key}' returned {value}; expected {required}.");
        }
    }
}
