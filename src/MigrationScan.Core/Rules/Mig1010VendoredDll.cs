using System.Xml;
using System.Xml.Linq;
using MigrationScan.Core.Engine;
using MigrationScan.Core.Models;

namespace MigrationScan.Core.Rules;

/// <summary>
/// MIG1010 — Vendored DLL with no source and no NuGet equivalent. Tier 1 (certain) on the
/// reference form: a <c>&lt;Reference&gt;</c> with a <c>&lt;HintPath&gt;</c> to a checked-in
/// assembly that is not restored from a NuGet package. These need per-assembly assessment —
/// many are Framework-only or ActiveX/COM interop with no supported successor.
/// </summary>
public sealed class Mig1010VendoredDll : ProjectRule
{
    public const string Id = "MIG1010";

    public Mig1010VendoredDll(RuleMetadata metadata) : base(metadata)
    {
    }

    public override IEnumerable<Finding> Analyze(AnalysisContext context)
    {
        foreach (XElement reference in context.Document.Descendants(context.Namespace + "Reference"))
        {
            if (reference.Element(context.Namespace + "HintPath")?.Value is not { } hintPath || !IsVendored(hintPath))
            {
                continue;
            }

            string name = reference.Attribute("Include")?.Value?.Split(',', 2)[0].Trim() ?? Path.GetFileName(hintPath);

            yield return Report(
                context,
                $"Project '{context.Project.Name}' references a vendored assembly ('{name}') from a checked-in " +
                $"path ('{hintPath.Trim()}'), not a NuGet package. Confirm it runs on modern .NET — many such " +
                "assemblies are Framework-only or ActiveX/COM with no supported successor.",
                line: LineOf(reference));
        }
    }

    // A HintPath into the NuGet package cache/folder is a normal package restore, not a vendored DLL.
    private static bool IsVendored(string hintPath)
    {
        string normalized = hintPath.Replace('\\', '/');
        return normalized.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
            && !normalized.Contains("/packages/", StringComparison.OrdinalIgnoreCase)
            && !normalized.Contains(".nuget", StringComparison.OrdinalIgnoreCase);
    }

    private static int? LineOf(XElement element) =>
        element is IXmlLineInfo info && info.HasLineInfo() ? info.LineNumber : null;
}
