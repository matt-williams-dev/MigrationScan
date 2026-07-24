using MigrationScan.Core.Engine;
using MigrationScan.Core.Models;

namespace MigrationScan.Core.Rules;

/// <summary>
/// MIG2002 — Package marked deprecated on nuget.org (<c>--online</c> only).
///
/// Reads deprecation status from the context's package registry, which is empty offline —
/// so this rule produces nothing unless <c>--online</c> is passed. The deprecation is an
/// authoritative fact from nuget.org, hence Tier 1 (certain).
/// </summary>
public sealed class Mig2002DeprecatedPackage : ProjectRule
{
    public const string Id = "MIG2002";

    public Mig2002DeprecatedPackage(RuleMetadata metadata) : base(metadata)
    {
    }

    public override IEnumerable<Finding> Analyze(AnalysisContext context)
    {
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);

        foreach (PackageReferenceInfo package in context.Packages)
        {
            if (!seen.Add(package.Id))
            {
                continue; // one finding per package, even if referenced more than once
            }

            if (context.PackageRegistry.Lookup(package.Id) is not { IsDeprecated: true } info)
            {
                continue;
            }

            string replacement = info.AlternatePackage is { } alternate
                ? $" The maintainers suggest replacing it with '{alternate}'."
                : string.Empty;

            yield return Report(
                context,
                $"Package '{package.Id}' is marked deprecated on nuget.org.{replacement}",
                file: package.DeclaredIn,
                line: package.Line);
        }
    }
}
