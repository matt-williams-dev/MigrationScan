using MigrationScan.Core.Engine;
using MigrationScan.Core.Models;

namespace MigrationScan.Core.Rules;

/// <summary>MIG4001 — System.Drawing.Common on non-Windows. Tier 2 (probable).</summary>
public sealed class Mig4001SystemDrawingCommon : SyntaxRule
{
    public const string Id = "MIG4001";

    public Mig4001SystemDrawingCommon(RuleMetadata metadata) : base(metadata)
    {
    }

    protected override IEnumerable<Finding> AnalyzeSource(SourceFile source, AnalysisContext context)
    {
        var root = source.SyntaxTree.GetRoot();
        if (SyntaxScan.FirstNamespaceUsageLine(root, "System.Drawing") is int line)
        {
            yield return Report(
                context,
                source,
                "Uses System.Drawing. On modern .NET, System.Drawing.Common is supported only on Windows " +
                "and throws PlatformNotSupportedException elsewhere. Use a cross-platform imaging library " +
                "(e.g. ImageSharp, SkiaSharp) if the app must run on Linux.",
                line);
        }
    }
}
