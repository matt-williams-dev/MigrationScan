using MigrationScan.Core.Engine;
using MigrationScan.Core.Models;

namespace MigrationScan.Core.Rules;

/// <summary>
/// MIG3001 — ASP.NET WebForms.
///
/// Tier 1 (certain): the presence of <c>.aspx</c>, <c>.ascx</c>, or <c>.master</c> files
/// identifies a WebForms application. WebForms has no equivalent on modern .NET, so this
/// is a blocker.
/// </summary>
public sealed class Mig3001WebForms : ProjectRule
{
    public const string Id = "MIG3001";

    internal static readonly string[] WebFormsExtensions = [".aspx", ".ascx", ".master"];

    public Mig3001WebForms(RuleMetadata metadata) : base(metadata)
    {
    }

    public override IEnumerable<Finding> Analyze(AnalysisContext context)
    {
        if (!context.HasFilesWithExtension(WebFormsExtensions))
        {
            yield break;
        }

        yield return Report(
            context,
            $"Project '{context.Project.Name}' is an ASP.NET WebForms application (.aspx/.ascx present). " +
            "WebForms has no equivalent on modern .NET and needs re-architecting (e.g. to Razor Pages, MVC, or Blazor).",
            line: context.Project.RootElementLine);
    }
}
