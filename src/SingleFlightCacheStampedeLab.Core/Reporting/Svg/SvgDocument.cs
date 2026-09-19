using System.Globalization;
using System.Xml.Linq;

namespace SingleFlightCacheStampedeLab.Reporting.Svg;

internal static class SvgDocument
{
    public static XNamespace Namespace => "http://www.w3.org/2000/svg";
    public static string Number(double value)
    {
        if (!double.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value), "SVG coordinates must be finite.");
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    public static XElement Element(string name, params object[] content) => new(Namespace + name, content);
    public static XAttribute Attribute(string name, double value) => new(name, Number(value));

    public static XElement Text(double x, double y, string text, string color = "#526078")
        => Element("text", Attribute("x", x), Attribute("y", y), new XAttribute("fill", color), text);

    public static XElement Line(double x1, double y1, double x2, double y2, string color)
        => Element("line", Attribute("x1", x1), Attribute("y1", y1), Attribute("x2", x2),
            Attribute("y2", y2), new XAttribute("stroke", color));

    public static string Serialize(XElement root)
        => new XDeclaration("1.0", "utf-8", null) + "\n" + root.ToString();
}
