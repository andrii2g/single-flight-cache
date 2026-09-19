using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using SingleFlightCacheStampedeLab.Metrics;
using SingleFlightCacheStampedeLab.Reporting;
using SingleFlightCacheStampedeLab.Reporting.Svg;
using SingleFlightCacheStampedeLab.Sweeps;
using SingleFlightCacheStampedeLab.Workloads;
using Xunit;

namespace SingleFlightCacheStampedeLab.Tests.Reporting;

public sealed class SvgReportWriterTests
{
    [Fact]
    public void SvgEscapesLabelsAndHasFiniteCoordinatesAndAllSeries()
    {
        var chart = new LineChart("A & B < C > D", "X < Y", "Y & Z",
            [
                new("Naive & co", [new(1, 2), new(2, 4)]),
                new("Global < lock", [new(1, 1), new(2, 1)]),
                new("Single > flight", [new(1, 1), new(2, 1)])
            ]);
        string svg = chart.Render();
        Assert.StartsWith("<?xml", svg, StringComparison.Ordinal);
        XDocument xml = XDocument.Parse(svg);
        XNamespace ns = "http://www.w3.org/2000/svg";
        Assert.Equal(ns + "svg", xml.Root!.Name);
        Assert.Equal("A & B < C > D", xml.Root.Element(ns + "title")!.Value);
        Assert.Contains("&amp;", svg, StringComparison.Ordinal);
        Assert.Contains("&lt;", svg, StringComparison.Ordinal);
        Assert.Contains("&gt;", svg, StringComparison.Ordinal);
        Assert.Equal(3, xml.Descendants(ns + "g").Count(element => (string?)element.Attribute("class") == "series"));
        Assert.Empty(xml.Descendants(ns + "script"));
        Assert.DoesNotContain("NaN", svg, StringComparison.Ordinal);
        Assert.DoesNotContain("Infinity", svg, StringComparison.Ordinal);
        Assert.True(double.Parse(xml.Root.Attribute("width")!.Value, CultureInfo.InvariantCulture) > 0);
        Assert.True(double.Parse(xml.Root.Attribute("height")!.Value, CultureInfo.InvariantCulture) > 0);
        Assert.Equal("0 0 960 580", xml.Root.Attribute("viewBox")!.Value);
        Assert.Throws<ArgumentException>(() => (chart with
        {
            Series = [new("invalid", [new(double.NaN, 0)])]
        }).Render());
    }

    [Fact]
    public async Task JsonRoundTripsCsvColumnsAreStableAndChartsAreWritten()
    {
        ExperimentReport report = await SmallReportAsync();
        DirectoryInfo directory = Directory.CreateTempSubdirectory("singleflight-report-");
        try
        {
            string jsonPath = Path.Combine(directory.FullName, "summary.json");
            string csvPath = Path.Combine(directory.FullName, "raw-results.csv");
            await JsonSummaryWriter.WriteAsync(jsonPath, report, TestContext.Current.CancellationToken);
            await CsvResultWriter.WriteAsync(csvPath, report.Runs, TestContext.Current.CancellationToken);
            await SvgReportWriter.WriteAsync(directory.FullName, report.Summaries, TestContext.Current.CancellationToken);
            string json = await File.ReadAllTextAsync(jsonPath, TestContext.Current.CancellationToken);
            ExperimentReport copy = JsonSerializer.Deserialize<ExperimentReport>(json, JsonSummaryWriter.CreateOptions())!;
            Assert.Equal(report.Profile, copy.Profile);
            Assert.Equal(report.Runs, copy.Runs);
            Assert.Equal(report.Summaries, copy.Summaries);
            Assert.Contains("\"createdAt\"", json, StringComparison.Ordinal);
            string[] csv = await File.ReadAllLinesAsync(csvPath, TestContext.Current.CancellationToken);
            const string requiredColumns = "scenario,strategy,concurrency,backend_ms,iteration,actual_loads,expected_loads,duplicate_loads,amplification,throughput,p50_ms,p95_ms,p99_ms,max_ms,lock_contentions,unrelated_key_wait_ms,flights_created,coalesced_callers,peak_waiters";
            Assert.StartsWith(requiredColumns + ",", csv[0], StringComparison.Ordinal);
            Assert.Equal(report.Runs.Count + 1, csv.Length);
            Assert.All(csv.Skip(1), row => Assert.Equal(csv[0].Split(',').Length, row.Split(',').Length));
            foreach (string path in Directory.GetFiles(directory.FullName, "*.svg"))
            {
                XDocument xml = XDocument.Load(path);
                Assert.NotNull(xml.Root);
            }

            Assert.Equal(3, Directory.GetFiles(directory.FullName, "*.svg").Length);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Fact]
    public async Task CsvUsesInvariantDecimalsAndQuotesFields()
    {
        ExperimentReport report = await SmallReportAsync();
        RunRow first = report.Runs[0] with { Strategy = "test,\"quoted\"", BackendMs = 1.5 };
        DirectoryInfo directory = Directory.CreateTempSubdirectory("singleflight-csv-");
        CultureInfo previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            string path = Path.Combine(directory.FullName, "result.csv");
            await CsvResultWriter.WriteAsync(path, [first], TestContext.Current.CancellationToken);
            string csv = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
            Assert.Contains("\"test,\"\"quoted\"\"\"", csv, StringComparison.Ordinal);
            Assert.Contains(",1.5,", csv, StringComparison.Ordinal);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
            directory.Delete(true);
        }
    }

    internal static Task<ExperimentReport> SmallReportAsync() => new SweepRunner().RunAsync(
        new(new("test", [ScenarioKind.HotKeyBurst], [1, 4], [0], 2),
            [StrategyKind.Naive, StrategyKind.GlobalLock, StrategyKind.SingleFlight], TimeSpan.FromSeconds(1), []),
        cancellationToken: TestContext.Current.CancellationToken);
}
