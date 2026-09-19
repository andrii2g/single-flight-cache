using SingleFlightCacheStampedeLab.Cli;
using SingleFlightCacheStampedeLab.Sweeps;
using SingleFlightCacheStampedeLab.Workloads;
using Xunit;

namespace SingleFlightCacheStampedeLab.Tests.Cli;

public sealed class CliOptionsTests
{
    [Theory]
    [InlineData("unknown")]
    [InlineData("scenario --concurrency 0")]
    [InlineData("scenario --concurrency -1")]
    [InlineData("scenario --concurrency 2147483648")]
    [InlineData("scenario --backend-ms -1")]
    [InlineData("scenario --ttl-ms 0")]
    [InlineData("scenario --iterations nope")]
    [InlineData("scenario --strategy other")]
    [InlineData("scenario --scenario other")]
    [InlineData("sweep --profile other")]
    [InlineData("quick --profile full")]
    [InlineData("sweep --strategy all")]
    [InlineData("quick --concurrency 4")]
    [InlineData("scenario --profile quick")]
    [InlineData("scenario --backend-ms 1 --backend-ms 2")]
    [InlineData("scenario --unknown 1")]
    [InlineData("scenario --output")]
    [InlineData("help --output output")]
    [InlineData("scenario --scenario unrelated-key-blocking --concurrency 4")]
    public void RejectsMalformedOrIncompatibleOptions(string command)
        => Assert.Throws<ArgumentException>(() => CliOptions.Parse(command.Split(' ')));

    [Fact]
    public void ParsesDocumentedCommands()
    {
        Assert.Equal("help", CliOptions.Parse([]).Command);
        Assert.Equal(3, CliOptions.Parse(["quick"]).Iterations);
        Assert.Equal(5, CliOptions.Parse(["sweep", "--profile", "full"]).Iterations);
        CliOptions options = CliOptions.Parse(["scenario", "--scenario", "unrelated-key-blocking",
            "--strategy", "singleflight", "--backend-ms", "0", "--output", "artifacts/test"]);
        Assert.Equal(ScenarioKind.UnrelatedKeyBlocking, options.Scenario);
        Assert.Equal(2, options.Concurrency);
        Assert.Equal(0, options.BackendMs);
        Assert.Equal(StrategyKind.SingleFlight, Assert.Single(options.Strategies));
        Assert.True(Path.IsPathFullyQualified(options.Output));
    }
}
