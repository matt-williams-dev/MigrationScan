using MigrationScan.Core.Engine;
using MigrationScan.Core.Models;

namespace MigrationScan.Core.Rules;

/// <summary>
/// MIG1002 — <c>packages.config</c> instead of PackageReference.
///
/// Tier 1 (certain): the presence of a <c>packages.config</c> beside the project is
/// read straight from disk.
/// </summary>
public sealed class Mig1002PackagesConfig : ProjectRule
{
    public const string Id = "MIG1002";

    public Mig1002PackagesConfig(RuleMetadata metadata) : base(metadata)
    {
    }

    public override IEnumerable<Finding> Analyze(AnalysisContext context)
    {
        if (!context.HasSiblingFile("packages.config"))
        {
            yield break;
        }

        string relativePath = context.ToRelative(Path.Combine(context.ProjectDirectory, "packages.config"));
        yield return Report(
            context,
            $"Project '{context.Project.Name}' declares NuGet dependencies in packages.config rather than PackageReference.",
            file: relativePath,
            line: 1);
    }
}
