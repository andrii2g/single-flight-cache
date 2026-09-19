namespace SingleFlightCacheStampedeLab.Cli;

internal static class HelpText
{
    public const string Value = """
        singleflight-cache-stampede-lab — .NET 10 async coordination experiments

        Commands:
          quick                      Quick matrix plus a two-key blocking diagnostic
          sweep --profile quick|full Quick or full matrix
          scenario                   One scenario (default: hot-key, all strategies)
          help                       Show this help

        Common experiment options:
          --ttl-ms <positive int>       Cache TTL (default: 1000)
          --iterations <positive int>   Recorded iterations (quick: 3, full: 5, scenario: 1)
          --output <directory>          Write artifacts here; default: artifacts/<UTC>-<profile>

        scenario options:
          --scenario hot-key|simultaneous-expiration|mixed-keys|independent-keys|unrelated-key-blocking
          --strategy naive|global-lock|singleflight|all
          --concurrency <positive int>  Default: 128; unrelated-key-blocking requires 2
          --backend-ms <nonnegative int> Default: 20

        Each run has fresh state. One warm-up per strategy is excluded.
        Tables and charts show per-scalar medians; CSV preserves every recorded iteration.
        The full matrix deliberately serializes cold keys under GlobalLock and takes many minutes.
        Ctrl+C cancels the experiment. Exit codes: 0 success, 1 experiment failure/cancellation, 2 usage.
        """;
}
