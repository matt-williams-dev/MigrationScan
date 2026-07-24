using MigrationScan.Core.Engine;
using MigrationScan.Core.Models;

namespace MigrationScan.Core.Rules;

/// <summary>
/// MIG3015 — WCF usage (<c>System.ServiceModel</c>). Tier 2 (probable).
///
/// The companion to <see cref="Mig3004WcfServiceHost"/>: any use of <c>System.ServiceModel</c>
/// (typically a WCF client / service reference) needs the <c>System.ServiceModel.*</c> client
/// packages on modern .NET. Client usage is supported that way; server-side hosting is not and
/// needs CoreWCF (MIG3004).
/// </summary>
public sealed class Mig3015WcfClient : SyntaxRule
{
    public const string Id = "MIG3015";

    public Mig3015WcfClient(RuleMetadata metadata) : base(metadata)
    {
    }

    protected override IEnumerable<Finding> AnalyzeSource(SourceFile source, AnalysisContext context)
    {
        var root = source.SyntaxTree.GetRoot();
        if (SyntaxScan.FirstNamespaceUsageLine(root, "System.ServiceModel") is int line)
        {
            yield return Report(
                context,
                source,
                "Uses WCF (System.ServiceModel). On modern .NET the WCF client is supported by adding the " +
                "System.ServiceModel.* packages (e.g. System.ServiceModel.Http, .NetTcp) and regenerating the " +
                "proxy; config-based endpoints usually move to code. Server-side hosting instead needs CoreWCF (see MIG3004).",
                line);
        }
    }
}
