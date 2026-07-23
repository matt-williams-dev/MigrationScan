using MigrationScan.Core.Engine;
using MigrationScan.Core.Models;

namespace MigrationScan.Core.Rules;

/// <summary>MIG8002 — <c>Encoding.Default</c> behavior change. Tier 2 (probable).</summary>
public sealed class Mig8002EncodingDefault : SyntaxRule
{
    public const string Id = "MIG8002";

    public Mig8002EncodingDefault(RuleMetadata metadata) : base(metadata)
    {
    }

    protected override IEnumerable<Finding> AnalyzeSource(SourceFile source, AnalysisContext context)
    {
        var root = source.SyntaxTree.GetRoot();
        foreach (int line in SyntaxScan.MemberAccessLines(root, "Encoding", "Default"))
        {
            yield return Report(
                context,
                source,
                "Reads Encoding.Default. On .NET Framework this is the system ANSI code page; on modern .NET " +
                "it is always UTF-8. Code that assumed ANSI will read/write bytes differently after migration.",
                line);
        }
    }
}
