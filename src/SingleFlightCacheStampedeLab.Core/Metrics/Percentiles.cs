namespace SingleFlightCacheStampedeLab.Metrics;

public static class Percentiles
{
    /// <summary>Nearest rank: sorted[max(0, ceil(p / 100 * n) - 1)]; p is in [0, 100].</summary>
    public static double NearestRank(IEnumerable<double> values, double percentile)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (!double.IsFinite(percentile) || percentile < 0 || percentile > 100)
            throw new ArgumentOutOfRangeException(nameof(percentile));
        double[] sorted = values.Order().ToArray();
        if (sorted.Length == 0) throw new ArgumentException("Percentiles require at least one observation.", nameof(values));
        if (sorted.Any(value => !double.IsFinite(value)))
            throw new ArgumentException("Percentiles require finite observations.", nameof(values));
        int index = Math.Max(0, (int)Math.Ceiling(percentile / 100 * sorted.Length) - 1);
        return sorted[index];
    }

    /// <summary>For repeated-run aggregation: average the two middle values for even counts.</summary>
    public static double Median(IEnumerable<double> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        double[] sorted = values.Order().ToArray();
        if (sorted.Length == 0 || sorted.Any(value => !double.IsFinite(value)))
            throw new ArgumentException("Medians require finite, nonempty observations.", nameof(values));
        int middle = sorted.Length / 2;
        return sorted.Length % 2 == 1 ? sorted[middle] : sorted[middle - 1] / 2 + sorted[middle] / 2;
    }
}
