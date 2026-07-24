using MigrationScan.Core.Engine;
using MigrationScan.Core.Models;

namespace MigrationScan.Core.Rules;

/// <summary>MIG7006 — LINQ to SQL (<c>System.Data.Linq</c>). Tier 2 (probable). No modern-.NET equivalent.</summary>
public sealed class Mig7006LinqToSql : SyntaxRule
{
    public const string Id = "MIG7006";

    public Mig7006LinqToSql(RuleMetadata metadata) : base(metadata)
    {
    }

    protected override IEnumerable<Finding> AnalyzeSource(SourceFile source, AnalysisContext context)
    {
        var root = source.SyntaxTree.GetRoot();
        foreach (int line in SyntaxScan.NamespaceUsageLines(root, "System.Data.Linq"))
        {
            yield return Report(
                context,
                source,
                "Uses LINQ to SQL (System.Data.Linq), which is not available on modern .NET. Migrate the data " +
                "layer to Entity Framework Core or another supported ORM.",
                line);
        }
    }
}
