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

    // Assemblies shipped with the framework itself — not third-party GAC dependencies.
    private static readonly string[] FrameworkPrefixes =
    [
        "System", "mscorlib", "netstandard", "Microsoft.CSharp", "Microsoft.VisualBasic",
        "Microsoft.Win32", "PresentationCore", "PresentationFramework", "WindowsBase",
        "UIAutomationProvider", "UIAutomationTypes", "ReachFramework",
    ];

    public Mig1005GacReference(RuleMetadata metadata) : base(metadata)
    {
    }

    public override IEnumerable<Finding> Analyze(AnalysisContext context)
    {
        foreach (AssemblyReferenceInfo reference in context.AssemblyReferences)
        {
            // GAC-resolved third-party assembly: strong-named, no HintPath, not a framework assembly.
            if (reference.HasHintPath || !reference.IsStrongNamed || IsFrameworkAssembly(reference.SimpleName))
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

    private static bool IsFrameworkAssembly(string simpleName) =>
        FrameworkPrefixes.Any(prefix =>
            simpleName.Equals(prefix, StringComparison.OrdinalIgnoreCase)
            || simpleName.StartsWith(prefix + ".", StringComparison.OrdinalIgnoreCase));
}
