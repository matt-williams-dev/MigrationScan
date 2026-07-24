using MigrationScan.Core.Engine;
using MigrationScan.Core.Models;

namespace MigrationScan.Core.Rules;

/// <summary>MIG4005 — <c>EventLog</c>. Tier 2 (probable). Windows-only on modern .NET.</summary>
public sealed class Mig4005EventLog : SyntaxRule
{
    public const string Id = "MIG4005";

    public Mig4005EventLog(RuleMetadata metadata) : base(metadata)
    {
    }

    protected override IEnumerable<Finding> AnalyzeSource(SourceFile source, AnalysisContext context)
    {
        var root = source.SyntaxTree.GetRoot();
        foreach (int line in SyntaxScan.IdentifierLines(root, "EventLog"))
        {
            yield return Report(
                context,
                source,
                "Writes to the Windows Event Log (System.Diagnostics.EventLog), which is Windows-only on modern " +
                ".NET. Use a cross-platform logging framework (e.g. Microsoft.Extensions.Logging) instead.",
                line);
        }
    }
}
