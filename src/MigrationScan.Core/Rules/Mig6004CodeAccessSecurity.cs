using MigrationScan.Core.Engine;
using MigrationScan.Core.Models;

namespace MigrationScan.Core.Rules;

/// <summary>MIG6004 — Code Access Security attributes. Tier 2 (probable). CAS is obsolete on modern .NET.</summary>
public sealed class Mig6004CodeAccessSecurity : SyntaxRule
{
    public const string Id = "MIG6004";

    public Mig6004CodeAccessSecurity(RuleMetadata metadata) : base(metadata)
    {
    }

    protected override IEnumerable<Finding> AnalyzeSource(SourceFile source, AnalysisContext context)
    {
        var root = source.SyntaxTree.GetRoot();
        foreach (int line in SyntaxScan.AttributeLines(root, "SecurityPermission", "PermissionSet", "PrincipalPermission"))
        {
            yield return Report(
                context,
                source,
                "Uses a Code Access Security attribute (e.g. SecurityPermission/PermissionSet). CAS is not " +
                "honored on modern .NET; these attributes are obsolete and should be removed.",
                line);
        }
    }
}
