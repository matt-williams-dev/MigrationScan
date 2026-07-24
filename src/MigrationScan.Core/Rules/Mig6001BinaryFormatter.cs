using MigrationScan.Core.Engine;
using MigrationScan.Core.Models;

namespace MigrationScan.Core.Rules;

/// <summary>MIG6001 — <c>BinaryFormatter</c>. Tier 2 (probable). Removed in .NET 9 — a hard blocker.</summary>
public sealed class Mig6001BinaryFormatter : SyntaxRule
{
    public const string Id = "MIG6001";

    public Mig6001BinaryFormatter(RuleMetadata metadata) : base(metadata)
    {
    }

    protected override IEnumerable<Finding> AnalyzeSource(SourceFile source, AnalysisContext context)
    {
        var root = source.SyntaxTree.GetRoot();
        foreach (int line in SyntaxScan.IdentifierLines(root, "BinaryFormatter"))
        {
            yield return Report(
                context,
                source,
                "Uses BinaryFormatter, which is removed in .NET 9 and throws when used. Replace it with a " +
                "safe serializer such as System.Text.Json, MessagePack, or protobuf.",
                line);
        }
    }
}
