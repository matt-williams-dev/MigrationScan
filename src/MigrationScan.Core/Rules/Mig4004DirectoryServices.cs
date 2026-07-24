using MigrationScan.Core.Engine;
using MigrationScan.Core.Models;

namespace MigrationScan.Core.Rules;

/// <summary>MIG4004 — System.DirectoryServices / Active Directory. Tier 2 (probable).</summary>
public sealed class Mig4004DirectoryServices : SyntaxRule
{
    public const string Id = "MIG4004";

    public Mig4004DirectoryServices(RuleMetadata metadata) : base(metadata)
    {
    }

    protected override IEnumerable<Finding> AnalyzeSource(SourceFile source, AnalysisContext context)
    {
        var root = source.SyntaxTree.GetRoot();
        foreach (int line in SyntaxScan.NamespaceUsageLines(root, "System.DirectoryServices"))
        {
            yield return Report(
                context,
                source,
                "Uses System.DirectoryServices (Active Directory). On modern .NET this is Windows-only; " +
                "it throws PlatformNotSupportedException on other platforms.",
                line);
        }
    }
}
