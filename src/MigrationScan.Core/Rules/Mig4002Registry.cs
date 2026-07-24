using MigrationScan.Core.Engine;
using MigrationScan.Core.Models;

namespace MigrationScan.Core.Rules;

/// <summary>
/// MIG4002 — Windows Registry access. Tier 2 (probable): a type named <c>Registry</c> or
/// <c>RegistryKey</c> could be the user's own, so this is reported as probable (spec §5).
/// </summary>
public sealed class Mig4002Registry : SyntaxRule
{
    public const string Id = "MIG4002";

    public Mig4002Registry(RuleMetadata metadata) : base(metadata)
    {
    }

    protected override IEnumerable<Finding> AnalyzeSource(SourceFile source, AnalysisContext context)
    {
        var root = source.SyntaxTree.GetRoot();
        foreach (int line in SyntaxScan.IdentifierLines(root, "Registry", "RegistryKey"))
        {
            yield return Report(
                context,
                source,
                "Accesses the Windows Registry (Microsoft.Win32.Registry). The registry does not exist on " +
                "non-Windows platforms; the call throws PlatformNotSupportedException there.",
                line);
        }
    }
}
