using MigrationScan.Core.Engine;
using MigrationScan.Core.Models;

namespace MigrationScan.Core.Rules;

/// <summary>MIG3004 — WCF service host (server side). Tier 2 (probable).</summary>
public sealed class Mig3004WcfServiceHost : SyntaxRule
{
    public const string Id = "MIG3004";

    public Mig3004WcfServiceHost(RuleMetadata metadata) : base(metadata)
    {
    }

    protected override IEnumerable<Finding> AnalyzeSource(SourceFile source, AnalysisContext context)
    {
        var root = source.SyntaxTree.GetRoot();
        foreach (int line in SyntaxScan.IdentifierLines(root, "ServiceHost"))
        {
            yield return Report(
                context,
                source,
                "Hosts a WCF service via ServiceHost. Server-side WCF is not part of modern .NET; " +
                "move the service to CoreWCF or re-implement the endpoint (e.g. gRPC or a minimal API).",
                line);
        }
    }
}
