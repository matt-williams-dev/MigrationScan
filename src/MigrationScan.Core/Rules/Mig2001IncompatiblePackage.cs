using MigrationScan.Core.Engine;
using MigrationScan.Core.Models;

namespace MigrationScan.Core.Rules;

/// <summary>
/// MIG2001 — Package has no version supporting the target framework.
///
/// Offline path: checks each referenced package against the bundled incompatibility
/// catalog. The <c>--online</c> path (Phase 6) supplements this with live nuget.org
/// lookups. Tier 1 (certain) — the package usage is read from project data, and matches
/// are curated known-incompatible packages.
/// </summary>
public sealed class Mig2001IncompatiblePackage : ProjectRule
{
    public const string Id = "MIG2001";

    private readonly PackageCompatibilityCatalog _packages;

    public Mig2001IncompatiblePackage(RuleMetadata metadata, PackageCompatibilityCatalog packages)
        : base(metadata) => _packages = packages;

    public override IEnumerable<Finding> Analyze(AnalysisContext context)
    {
        foreach (PackageReferenceInfo package in context.Packages)
        {
            if (_packages.Find(package.Id) is not { } incompatible)
            {
                continue;
            }

            yield return Report(
                context,
                $"Package '{package.Id}' has no version that supports {context.TargetFramework}. " +
                $"{incompatible.Reason} Consider: {incompatible.Replacement}.",
                file: package.DeclaredIn,
                line: package.Line);
        }
    }
}
