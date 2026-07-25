using MigrationScan.Core.Discovery;
using MigrationScan.Core.Engine;
using MigrationScan.Core.Models;

namespace MigrationScan.Core.Rules;

/// <summary>
/// MIG1005 — GAC reference (no HintPath).
///
/// A strong-named <c>&lt;Reference&gt;</c> with no <c>&lt;HintPath&gt;</c> resolves from the
/// Global Assembly Cache, which does not exist on modern .NET. Framework assemblies are
/// excluded (they are handled by the framework-specific rules). Tier 1 (certain) on the
/// reference form; see the rule doc for the false-positive note.
/// </summary>
public sealed class Mig1005GacReference : ProjectRule
{
    public const string Id = "MIG1005";

    public Mig1005GacReference(RuleMetadata metadata) : base(metadata)
    {
    }

    public override IEnumerable<Finding> Analyze(AnalysisContext context)
    {
        foreach (AssemblyReferenceInfo reference in context.AssemblyReferences)
        {
            // GAC-resolved third-party assembly: strong-named, no HintPath, not a framework assembly.
            if (reference.HasHintPath || !reference.IsStrongNamed || FrameworkAssemblies.Contains(reference.SimpleName))
            {
                continue;
            }

            yield return Report(
                context,
                $"Assembly '{reference.SimpleName}' is referenced from the GAC (strong-named, no HintPath). The GAC does not exist on modern .NET.",
                file: context.ProjectRelativePath,
                line: reference.Line);
        }
    }
}
