using MigrationScan.Core.Engine;
using MigrationScan.Core.Models;

namespace MigrationScan.Core.Rules;

/// <summary>
/// MIG4008 — <c>Thread.Abort</c>. Tier 2 (probable): matches any argument-less
/// <c>.Abort()</c> invocation, which could be an unrelated method (spec §5).
/// </summary>
public sealed class Mig4008ThreadAbort : SyntaxRule
{
    public const string Id = "MIG4008";

    public Mig4008ThreadAbort(RuleMetadata metadata) : base(metadata)
    {
    }

    protected override IEnumerable<Finding> AnalyzeSource(SourceFile source, AnalysisContext context)
    {
        var root = source.SyntaxTree.GetRoot();
        foreach (int line in SyntaxScan.ArglessInvocationLines(root, "Abort"))
        {
            yield return Report(
                context,
                source,
                "Calls Abort() (likely Thread.Abort). Thread.Abort is not supported on modern .NET and throws " +
                "PlatformNotSupportedException. Use cooperative cancellation (CancellationToken) instead.",
                line);
        }
    }
}
