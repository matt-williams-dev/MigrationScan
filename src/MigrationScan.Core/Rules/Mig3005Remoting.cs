using MigrationScan.Core.Engine;
using MigrationScan.Core.Models;

namespace MigrationScan.Core.Rules;

/// <summary>MIG3005 — .NET Remoting. Tier 2 (probable). Removed entirely from modern .NET.</summary>
public sealed class Mig3005Remoting : SyntaxRule
{
    public const string Id = "MIG3005";

    public Mig3005Remoting(RuleMetadata metadata) : base(metadata)
    {
    }

    protected override IEnumerable<Finding> AnalyzeSource(SourceFile source, AnalysisContext context)
    {
        var root = source.SyntaxTree.GetRoot();
        foreach (int line in SyntaxScan.UsingNamespaceLines(root, "System.Runtime.Remoting"))
        {
            yield return Report(
                context,
                source,
                "Uses .NET Remoting (System.Runtime.Remoting), which was removed from modern .NET. " +
                "Replace with a supported transport such as gRPC, WCF-on-CoreWCF, or an HTTP API.",
                line);
        }
    }
}
