using MigrationScan.Core.Engine;
using MigrationScan.Core.Models;

namespace MigrationScan.Core.Rules;

/// <summary>
/// MIG3002 — <c>System.Web</c> dependency outside WebForms.
///
/// A reference to the <c>System.Web</c> assembly in a project that is not a WebForms app
/// (those are owned by MIG3001). <c>System.Web</c> is not available on modern .NET.
/// Tier 1 (certain): the assembly reference is read from the project file.
/// </summary>
public sealed class Mig3002SystemWeb : ProjectRule
{
    public const string Id = "MIG3002";

    public Mig3002SystemWeb(RuleMetadata metadata) : base(metadata)
    {
    }

    public override IEnumerable<Finding> Analyze(AnalysisContext context)
    {
        // WebForms apps also reference System.Web; MIG3001 reports those.
        if (context.HasFilesWithExtension(Mig3001WebForms.WebFormsExtensions))
        {
            yield break;
        }

        AssemblyReferenceInfo? systemWeb = context.AssemblyReferences
            .FirstOrDefault(r => r.SimpleName.Equals("System.Web", StringComparison.OrdinalIgnoreCase));

        if (systemWeb is null)
        {
            yield break;
        }

        yield return Report(
            context,
            $"Project '{context.Project.Name}' references System.Web outside of WebForms. " +
            "System.Web is not available on modern .NET; the dependent code needs replacing.",
            line: systemWeb.Line);
    }
}
