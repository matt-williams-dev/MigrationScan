using MigrationScan.Core.Engine;
using MigrationScan.Core.Models;

namespace MigrationScan.Core.Rules;

/// <summary>MIG3010 — ASP.NET MVC 5 (System.Web.Mvc). Tier 2 (probable).</summary>
public sealed class Mig3010AspNetMvc5 : SyntaxRule
{
    public const string Id = "MIG3010";

    public Mig3010AspNetMvc5(RuleMetadata metadata) : base(metadata)
    {
    }

    protected override IEnumerable<Finding> AnalyzeSource(SourceFile source, AnalysisContext context)
    {
        var root = source.SyntaxTree.GetRoot();
        if (SyntaxScan.FirstNamespaceUsageLine(root, "System.Web.Mvc") is int line)
        {
            yield return Report(
                context,
                source,
                "References ASP.NET MVC 5 (System.Web.Mvc), which runs only on .NET Framework. " +
                "Migrate to ASP.NET Core MVC (Microsoft.AspNetCore.Mvc).",
                line);
        }
    }
}
