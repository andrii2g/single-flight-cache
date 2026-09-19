using System.Xml.Linq;

namespace SingleFlightCacheStampedeLab.Reporting.Svg;

public sealed record ChartPoint(double X, double Y);
public sealed record ChartSeries(string Name, IReadOnlyList<ChartPoint> Points);
public sealed record LineChart(
    string Title, string XLabel, string YLabel, IReadOnlyList<ChartSeries> Series,
    string Subtitle = "")
{
    public string Render()
    {
        ArgumentNullException.ThrowIfNull(Series);
        ChartPoint[] points = Series.SelectMany(series => series.Points).ToArray();
        if (points.Any(point => !double.IsFinite(point.X) || !double.IsFinite(point.Y) || point.X < 0 || point.Y < 0))
            throw new ArgumentException("Charts require finite nonnegative coordinates.", nameof(Series));
        const double width = 960, height = 580, left = 95, top = 105, plotWidth = 810, plotHeight = 335;
        double maxX = Math.Max(1, points.Select(point => point.X).DefaultIfEmpty(1).Max());
        double maxY = Math.Max(1, points.Select(point => point.Y).DefaultIfEmpty(1).Max()) * 1.1;
        double X(double value) => left + value / maxX * plotWidth;
        double Y(double value) => top + plotHeight - value / maxY * plotHeight;
        XElement root = SvgDocument.Element("svg",
            new XAttribute("width", width), new XAttribute("height", height),
            new XAttribute("viewBox", "0 0 960 580"), new XAttribute("role", "img"),
            new XAttribute("aria-labelledby", "chart-title chart-description"),
            new XAttribute("font-family", "system-ui, sans-serif"), new XAttribute("font-size", "13"),
            SvgDocument.Element("title", new XAttribute("id", "chart-title"), Title),
            SvgDocument.Element("desc", new XAttribute("id", "chart-description"), Subtitle),
            SvgDocument.Element("rect", new XAttribute("width", "100%"), new XAttribute("height", "100%"),
                new XAttribute("rx", "16"), new XAttribute("fill", "#f8fafc")),
            SvgDocument.Text(32, 40, Title, "#17233b"), SvgDocument.Text(32, 65, Subtitle),
            SvgDocument.Text(left, 92, YLabel));
        for (int tick = 0; tick <= 5; tick++)
        {
            double x = maxX * tick / 5;
            double y = maxY * tick / 5;
            root.Add(SvgDocument.Line(left, Y(y), left + plotWidth, Y(y), "#dce3ed"));
            XElement yText = SvgDocument.Text(left - 12, Y(y) + 5, SvgDocument.Number(y));
            yText.Add(new XAttribute("text-anchor", "end"));
            root.Add(yText);
            XElement xText = SvgDocument.Text(X(x), top + plotHeight + 24, SvgDocument.Number(x));
            xText.Add(new XAttribute("text-anchor", "middle"));
            root.Add(xText);
        }

        root.Add(SvgDocument.Line(left, top + plotHeight, left + plotWidth, top + plotHeight, "#8795ab"));
        root.Add(SvgDocument.Text(left + plotWidth / 2 - 60, 494, XLabel));
        string[] colors = ["#d65c32", "#7458c9", "#087f8c", "#283e61"];
        string[] dashes = ["", "8 5", "3 4", "12 4"];
        for (int index = 0; index < Series.Count; index++)
        {
            ChartSeries series = Series[index];
            string color = colors[index % colors.Length];
            XElement group = SvgDocument.Element("g", new XAttribute("class", "series"),
                new XAttribute("data-name", series.Name));
            string coordinates = string.Join(' ', series.Points.OrderBy(point => point.X)
                .Select(point => $"{SvgDocument.Number(X(point.X))},{SvgDocument.Number(Y(point.Y))}"));
            group.Add(SvgDocument.Element("polyline", new XAttribute("points", coordinates),
                new XAttribute("fill", "none"), new XAttribute("stroke", color),
                new XAttribute("stroke-width", "2.5"), new XAttribute("stroke-dasharray", dashes[index % dashes.Length])));
            foreach (ChartPoint point in series.Points)
                group.Add(SvgDocument.Element("circle", SvgDocument.Attribute("cx", X(point.X)),
                    SvgDocument.Attribute("cy", Y(point.Y)), new XAttribute("r", "3.5"),
                    new XAttribute("fill", color),
                    SvgDocument.Element("title", $"{series.Name}: {SvgDocument.Number(point.X)}, {SvgDocument.Number(point.Y)}")));
            root.Add(group);
            double legendX = 95 + index * 250;
            root.Add(SvgDocument.Line(legendX, 539, legendX + 26, 539, color),
                SvgDocument.Text(legendX + 35, 544, series.Name, color));
        }

        if (points.Length == 0) root.Add(SvgDocument.Text(260, 280, "No matching scenario in this run."));
        return SvgDocument.Serialize(root);
    }
}
