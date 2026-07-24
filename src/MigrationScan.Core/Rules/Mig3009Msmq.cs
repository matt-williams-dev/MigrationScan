using MigrationScan.Core.Engine;
using MigrationScan.Core.Models;

namespace MigrationScan.Core.Rules;

/// <summary>MIG3009 — MSMQ (<c>System.Messaging</c>). Tier 2 (probable). No modern-.NET equivalent.</summary>
public sealed class Mig3009Msmq : SyntaxRule
{
    public const string Id = "MIG3009";

    public Mig3009Msmq(RuleMetadata metadata) : base(metadata)
    {
    }

    protected override IEnumerable<Finding> AnalyzeSource(SourceFile source, AnalysisContext context)
    {
        var root = source.SyntaxTree.GetRoot();
        if (SyntaxScan.FirstNamespaceUsageLine(root, "System.Messaging") is int line)
        {
            yield return Report(
                context,
                source,
                "Uses MSMQ (System.Messaging), which has no counterpart on modern .NET. Move to a supported " +
                "message queue (e.g. Azure Service Bus, RabbitMQ) or a third-party MSMQ-compatible library.",
                line);
        }
    }
}
