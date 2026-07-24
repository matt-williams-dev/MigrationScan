using MigrationScan.Core.Engine;
using MigrationScan.Core.Models;

namespace MigrationScan.Core.Rules;

/// <summary>
/// MIG5001 — <c>ConfigurationManager.AppSettings</c> usage.
///
/// Tier 2 (probable): matched on the syntax tree without a resolved compilation, so a
/// same-named member on an unrelated type would be a false positive. On modern .NET this
/// API moved to the <c>System.Configuration.ConfigurationManager</c> package and reads
/// from app.config rather than being built in.
/// </summary>
public sealed class Mig5001ConfigurationManagerAppSettings : SyntaxRule
{
    public const string Id = "MIG5001";

    public Mig5001ConfigurationManagerAppSettings(RuleMetadata metadata) : base(metadata)
    {
    }

    protected override IEnumerable<Finding> AnalyzeSource(SourceFile source, AnalysisContext context)
    {
        var root = source.SyntaxTree.GetRoot();

        // Matches `ConfigurationManager.AppSettings` and `...Configuration.ConfigurationManager.AppSettings`
        // (the member-access receiver's simple name is what's compared).
        foreach (int line in SyntaxScan.MemberAccessLines(root, "ConfigurationManager", "AppSettings"))
        {
            yield return Report(
                context,
                source,
                "Reads configuration via ConfigurationManager.AppSettings. On modern .NET this requires the " +
                "System.Configuration.ConfigurationManager package, or migration to Microsoft.Extensions.Configuration.",
                line);
        }
    }
}
