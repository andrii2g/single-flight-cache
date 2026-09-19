using System.Globalization;
using SingleFlightCacheStampedeLab.Sweeps;
using SingleFlightCacheStampedeLab.Workloads;

namespace SingleFlightCacheStampedeLab.Cli;

public sealed record CliOptions(
    string Command, string Profile, ScenarioKind Scenario, IReadOnlyList<StrategyKind> Strategies,
    int Concurrency, int BackendMs, int TtlMs, int Iterations, string Output)
{
    public static CliOptions Parse(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        string command = args.Length == 0 ? "help" : args[0];
        if (command is "-h" or "--help") command = "help";
        if (command is not ("help" or "quick" or "sweep" or "scenario"))
            throw new ArgumentException($"Unknown command '{command}'.");
        if (command == "help" && args.Length > 1)
            throw new ArgumentException("help does not accept options.");

        var options = new Dictionary<string, string>(StringComparer.Ordinal);
        for (int index = 1; index < args.Length; index += 2)
        {
            string name = args[index];
            if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
                throw new ArgumentException($"Missing value for '{name}'.");
            if (name is not ("--scenario" or "--strategy" or "--concurrency" or "--backend-ms" or
                "--ttl-ms" or "--iterations" or "--profile" or "--output"))
                throw new ArgumentException($"Unknown option '{name}'.");
            if (!options.TryAdd(name, args[index + 1])) throw new ArgumentException($"Duplicate option '{name}'.");
        }

        foreach (string name in options.Keys)
        {
            bool allowed = name is "--output" or "--ttl-ms" or "--iterations" ||
                command == "scenario" && name is "--scenario" or "--strategy" or "--concurrency" or "--backend-ms" ||
                command == "sweep" && name == "--profile";
            if (!allowed) throw new ArgumentException($"Option '{name}' is incompatible with '{command}'.");
        }

        string profile = options.GetValueOrDefault("--profile", "quick");
        if (profile is not ("quick" or "full")) throw new ArgumentException("--profile must be quick or full.");
        ScenarioKind scenario = options.GetValueOrDefault("--scenario", "hot-key") switch
        {
            "hot-key" => ScenarioKind.HotKeyBurst,
            "simultaneous-expiration" => ScenarioKind.SimultaneousExpiration,
            "mixed-keys" => ScenarioKind.MixedKeys,
            "independent-keys" => ScenarioKind.IndependentKeys,
            "unrelated-key-blocking" => ScenarioKind.UnrelatedKeyBlocking,
            string value => throw new ArgumentException($"Unknown scenario '{value}'.")
        };
        StrategyKind[] strategies = options.GetValueOrDefault("--strategy", "all") switch
        {
            "naive" => [StrategyKind.Naive],
            "global-lock" => [StrategyKind.GlobalLock],
            "singleflight" => [StrategyKind.SingleFlight],
            "all" => [StrategyKind.Naive, StrategyKind.GlobalLock, StrategyKind.SingleFlight],
            string value => throw new ArgumentException($"Unknown strategy '{value}'.")
        };
        int concurrency = Integer(options, "--concurrency", scenario == ScenarioKind.UnrelatedKeyBlocking ? 2 : 128, 1);
        if (scenario == ScenarioKind.UnrelatedKeyBlocking && concurrency != 2)
            throw new ArgumentException("unrelated-key-blocking requires --concurrency 2.");
        int backend = Integer(options, "--backend-ms", 20, 0);
        int ttl = Integer(options, "--ttl-ms", 1000, 1);
        int iterations = Integer(options, "--iterations", command == "scenario" ? 1 : profile == "full" ? 5 : 3, 1);
        string output = options.GetValueOrDefault("--output",
            Path.Combine("artifacts", DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + "-" +
                (command == "sweep" ? profile : command)));
        ArgumentException.ThrowIfNullOrWhiteSpace(output);
        if (output.IndexOfAny(Path.GetInvalidPathChars()) >= 0) throw new ArgumentException("Invalid output directory.");
        output = Path.GetFullPath(output);
        return new(command, profile, scenario, Array.AsReadOnly(strategies), concurrency, backend, ttl, iterations, output);
    }

    private static int Integer(Dictionary<string, string> options, string name, int fallback, int minimum)
    {
        if (!options.TryGetValue(name, out string? value)) return fallback;
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int parsed) || parsed < minimum)
            throw new ArgumentException($"{name} requires an integer >= {minimum}.");
        return parsed;
    }
}
