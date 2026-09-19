using SingleFlightCacheStampedeLab.Metrics;
using Xunit;

namespace SingleFlightCacheStampedeLab.Tests.Metrics;

public sealed class PercentilesTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(50, 2)]
    [InlineData(95, 4)]
    [InlineData(99, 4)]
    [InlineData(100, 4)]
    public void NearestRankUsesDocumentedConvention(double percentile, double expected)
        => Assert.Equal(expected, Percentiles.NearestRank([4, 1, 3, 2], percentile));

    [Fact]
    public void HandlesSingletonOddCountsAndMedianMidpoints()
    {
        Assert.Equal(7, Percentiles.NearestRank([7], 50));
        Assert.Equal(3, Percentiles.NearestRank([1, 2, 3, 4, 5], 50));
        Assert.Equal(2.5, Percentiles.Median([1, 2, 3, 4]));
        Assert.Equal(3, Percentiles.Median([1, 2, 3, 4, 5]));
        Assert.Throws<ArgumentException>(() => Percentiles.NearestRank([], 50));
        Assert.Throws<ArgumentException>(() => Percentiles.NearestRank([double.NaN], 50));
        Assert.Throws<ArgumentOutOfRangeException>(() => Percentiles.NearestRank([1], 101));
    }
}
