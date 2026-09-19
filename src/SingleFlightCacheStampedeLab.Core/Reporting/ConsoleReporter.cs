using SingleFlightCacheStampedeLab.Sweeps;

namespace SingleFlightCacheStampedeLab.Reporting;

public static class ConsoleReporter
{
    public static void WriteGroup(TextWriter writer, IReadOnlyList<RunSummary> group)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(group);
        if (group.Count == 0) return;
        RunSummary first = group[0];
        writer.WriteLine();
        writer.WriteLine(FormattableString.Invariant(
            $"{first.Scenario} | concurrency {first.Concurrency} | backend {first.BackendMs:0.###} ms | TTL {first.TtlMs:0.###} ms | median of {first.Iterations}"));
        writer.WriteLine("Strategy          Loads Duplicate Amplification   P50 ms   P95 ms   P99 ms      Req/s");
        writer.WriteLine("------------------------------------------------------------------------------------");
        foreach (RunSummary row in group)
        {
            var m = row.Median;
            writer.WriteLine(FormattableString.Invariant(
                $"{row.Strategy,-17} {m.ActualLoads,5:0.#} {m.DuplicateLoads,9:0.#} {m.Amplification,12:0.00}x {m.P50Ms,8:0.00} {m.P95Ms,8:0.00} {m.P99Ms,8:0.00} {m.Throughput,10:0.0}"));
        }

        writer.WriteLine("Coordination      Contended Lock wait ms Flights Coalesced Peak waiters Unrelated ms");
        foreach (RunSummary row in group)
        {
            var m = row.Median;
            writer.WriteLine(FormattableString.Invariant(
                $"{row.Strategy,-17} {m.LockContentions,9:0.#} {m.LockWaitMs,12:0.00} {m.FlightsCreated,7:0.#} {m.CoalescedCallers,9:0.#} {m.PeakWaiters,12:0.#} {m.UnrelatedKeyWaitMs,12:0.00}"));
        }
    }
}
