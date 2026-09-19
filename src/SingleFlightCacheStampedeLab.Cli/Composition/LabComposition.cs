using SingleFlightCacheStampedeLab.Cli;
using SingleFlightCacheStampedeLab.Reporting;
using SingleFlightCacheStampedeLab.Reporting.Svg;
using SingleFlightCacheStampedeLab.Sweeps;

namespace SingleFlightCacheStampedeLab.Composition;

internal static class LabComposition
{
    public static async Task RunAsync(CliOptions options, string[] args, CancellationToken token)
    {
        SweepProfile profile = options.Command == "scenario"
            ? new("scenario", [options.Scenario], [options.Concurrency], [options.BackendMs], options.Iterations)
            : (options.Profile == "full" ? SweepProfile.Full : SweepProfile.Quick) with { Iterations = options.Iterations };
        string[] safeArgs = (string[])args.Clone();
        for (int index = 0; index + 1 < safeArgs.Length; index++)
            if (safeArgs[index] == "--output") safeArgs[++index] = "(output directory)";
        var settings = new ExperimentSettings(profile, options.Strategies, TimeSpan.FromMilliseconds(options.TtlMs),
            Array.AsReadOnly(safeArgs), options.Command != "scenario");
        Directory.CreateDirectory(options.Output);
        Console.WriteLine("singleflight-cache-stampede-lab");
        Console.WriteLine("Warm-up: one unrecorded run per strategy. Running isolated experiments...");
        ExperimentReport report = await new SweepRunner().RunAsync(settings,
            group => ConsoleReporter.WriteGroup(Console.Out, group), token);
        await CsvResultWriter.WriteAsync(Path.Combine(options.Output, "raw-results.csv"), report.Runs, token);
        await JsonSummaryWriter.WriteAsync(Path.Combine(options.Output, "summary.json"), report, token);
        await SvgReportWriter.WriteAsync(options.Output, report.Summaries, token);
        Console.WriteLine();
        Console.WriteLine($"Artifacts: {options.Output}");
    }
}
