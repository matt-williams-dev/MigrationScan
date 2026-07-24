using System.Xml;
using System.Xml.Linq;
using MigrationScan.Core.Engine;
using MigrationScan.Core.Models;

namespace MigrationScan.Core.Rules;

/// <summary>
/// MIG1006 — COM reference or interop assembly. Tier 1 (certain): read from the project's
/// <c>&lt;COMReference&gt;</c> elements. COM interop is a Windows lock-in — supported on modern
/// .NET when targeting Windows (net-windows), but never off Windows.
/// </summary>
public sealed class Mig1006ComReference : ProjectRule
{
    public const string Id = "MIG1006";

    public Mig1006ComReference(RuleMetadata metadata) : base(metadata)
    {
    }

    public override IEnumerable<Finding> Analyze(AnalysisContext context)
    {
        foreach (XElement com in context.Document.Descendants(context.Namespace + "COMReference"))
        {
            string name = com.Attribute("Include")?.Value
                ?? com.Element(context.Namespace + "Guid")?.Value
                ?? "(unnamed)";

            yield return Report(
                context,
                $"Project '{context.Project.Name}' has a COM reference ('{name}'). COM interop works on modern " +
                ".NET only when targeting Windows (net-windows); it is a Windows lock-in and unavailable elsewhere.",
                line: LineOf(com));
        }
    }

    private static int? LineOf(XElement element) =>
        element is IXmlLineInfo info && info.HasLineInfo() ? info.LineNumber : null;
}
