using MigrationScan.Core.Engine;
using MigrationScan.Core.Models;

namespace MigrationScan.Core.Rules;

/// <summary>MIG4003 — <c>System.Management</c> / WMI. Tier 2 (probable). Windows-only on modern .NET.</summary>
public sealed class Mig4003Wmi : SyntaxRule
{
    public const string Id = "MIG4003";

    public Mig4003Wmi(RuleMetadata metadata) : base(metadata)
    {
    }

    protected override IEnumerable<Finding> AnalyzeSource(SourceFile source, AnalysisContext context)
    {
        var root = source.SyntaxTree.GetRoot();
        foreach (int line in SyntaxScan.NamespaceUsageLines(root, "System.Management"))
        {
            yield return Report(
                context,
                source,
                "Uses WMI (System.Management). On modern .NET this is Windows-only (via the " +
                "System.Management package) and throws PlatformNotSupportedException elsewhere.",
                line);
        }
    }
}
