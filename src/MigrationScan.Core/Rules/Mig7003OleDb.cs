using MigrationScan.Core.Engine;
using MigrationScan.Core.Models;

namespace MigrationScan.Core.Rules;

/// <summary>MIG7003 — <c>System.Data.OleDb</c> on non-Windows. Tier 2 (probable).</summary>
public sealed class Mig7003OleDb : SyntaxRule
{
    public const string Id = "MIG7003";

    public Mig7003OleDb(RuleMetadata metadata) : base(metadata)
    {
    }

    protected override IEnumerable<Finding> AnalyzeSource(SourceFile source, AnalysisContext context)
    {
        var root = source.SyntaxTree.GetRoot();
        foreach (int line in SyntaxScan.NamespaceUsageLines(root, "System.Data.OleDb"))
        {
            yield return Report(
                context,
                source,
                "Uses System.Data.OleDb. On modern .NET the OLE DB provider is Windows-only (via the " +
                "System.Data.OleDb package). Use a native provider (e.g. Microsoft.Data.SqlClient) where possible.",
                line);
        }
    }
}
