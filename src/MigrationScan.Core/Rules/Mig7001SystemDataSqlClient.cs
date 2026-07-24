using MigrationScan.Core.Engine;
using MigrationScan.Core.Models;

namespace MigrationScan.Core.Rules;

/// <summary>MIG7001 — <c>System.Data.SqlClient</c>. Tier 2 (probable). Superseded by Microsoft.Data.SqlClient.</summary>
public sealed class Mig7001SystemDataSqlClient : SyntaxRule
{
    public const string Id = "MIG7001";

    public Mig7001SystemDataSqlClient(RuleMetadata metadata) : base(metadata)
    {
    }

    protected override IEnumerable<Finding> AnalyzeSource(SourceFile source, AnalysisContext context)
    {
        var root = source.SyntaxTree.GetRoot();
        if (SyntaxScan.FirstNamespaceUsageLine(root, "System.Data.SqlClient") is int line)
        {
            yield return Report(
                context,
                source,
                "Uses System.Data.SqlClient, which is in maintenance mode. Switch to Microsoft.Data.SqlClient " +
                "(note its Encrypt=true default change — see MIG7002).",
                line);
        }
    }
}
