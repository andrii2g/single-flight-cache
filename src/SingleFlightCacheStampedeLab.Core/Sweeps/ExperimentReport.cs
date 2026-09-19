using System.Runtime.InteropServices;
using SingleFlightCacheStampedeLab.Metrics;
using SingleFlightCacheStampedeLab.Workloads;

namespace SingleFlightCacheStampedeLab.Sweeps;

public sealed record RunRow(
    ScenarioKind Scenario, string Strategy, int Concurrency, double BackendMs, double TtlMs,
    int Iteration, RunMetrics Metrics, int? ShuffleSeed, double WaveOffsetMs);

public sealed record RunSummary(
    ScenarioKind Scenario, string Strategy, int Concurrency, double BackendMs, double TtlMs,
    int Iterations, RunMetrics Median, int? ShuffleSeed, double WaveOffsetMs);

public sealed record EnvironmentMetadata(
    string Framework, string OperatingSystem, string Architecture, int ProcessorCount, bool InContainer)
{
    public static EnvironmentMetadata Capture() => new(
        RuntimeInformation.FrameworkDescription, RuntimeInformation.OSDescription,
        RuntimeInformation.ProcessArchitecture.ToString(), Environment.ProcessorCount,
        Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") is "true" or "1" ||
        Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINERS") is "true" or "1");
}

public sealed record ExperimentReport(
    DateTimeOffset CreatedAt, EnvironmentMetadata Environment, string Profile,
    IReadOnlyList<string> Arguments, string PercentileConvention, string Aggregation,
    int WarmupRuns, IReadOnlyList<RunRow> Runs, IReadOnlyList<RunSummary> Summaries);
