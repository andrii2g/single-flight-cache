# Recorded results

This snapshot comes from the full sweep completed on 2026-09-19 in the .NET 10 runtime
container through WSL: .NET 10.0.12, Ubuntu 24.04.5 LTS, x64, 16 reported logical
processors, a 1,000 ms TTL, and five recorded iterations per combination.

The full experiment contains 2,460 iterations. This directory retains the **255 raw
iteration rows** and **51 median summaries** supporting the charts and README table.
The SVGs are copied directly from the CLI output.

| File | Measurement |
| --- | --- |
| [backend-amplification.svg](backend-amplification.svg) | HotKeyBurst physical/logical load ratio, 100 ms backend cost, concurrency 1–512 |
| [p95-latency-vs-concurrency.svg](p95-latency-vs-concurrency.svg) | HotKeyBurst P95 request latency for the same combinations |
| [unrelated-key-blocking.svg](unrelated-key-blocking.svg) | Request B latency at backend costs of 1, 5, 20, and 100 ms |
| [raw-results.csv](raw-results.csv) | The recorded iterations used by these figures and the README table |
| [summary.json](summary.json) | Run metadata, selection rules, per-scalar medians, and SHA-256 of the complete original CSV |

For unrelated-key blocking, A starts first and B follows after the configured wave
offset. Both use the same backend cost. B latency excludes its initial offset.

P95 uses nearest rank within each iteration. Lines show medians across the five
iterations, not a pooled percentile. The same aggregation applies to the table.
Warm-up and priming are excluded. These are observations from one run; timer resolution
and scheduling affect timings, so they are not performance guarantees.

## Reproduce the experiment

From the repository root:

```sh
dotnet run --project src/SingleFlightCacheStampedeLab.Cli -c Release -- sweep --profile full --output artifacts/full
```

Or from WSL, using the runtime container:

```bash
docker compose run --build --rm lab sweep --profile full --output /app/artifacts/full
```

The CLI generates the three SVGs, the complete CSV, and a complete JSON summary.
To refresh this documentation snapshot, copy the SVGs and select the CSV rows and JSON
summaries using the rules in this directory's summary.json. Keep metadata, charts, and
the main README table from the same run. Do not replace only one chart with another run.

The full sweep takes many minutes because the global-lock independent-key cases
intentionally serialize every backend load. New runs will produce different timings.
