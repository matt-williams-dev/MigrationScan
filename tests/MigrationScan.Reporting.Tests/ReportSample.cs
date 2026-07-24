using MigrationScan.Core.Models;

namespace MigrationScan.Reporting.Tests;

/// <summary>
/// A representative, self-contained analysis result used by the golden-file report tests.
/// It deliberately exercises every section of the reports: multiple projects, all
/// severities, both tiers, a severity-blocker, an effort-blocker, the occurrence factor
/// (a rule appearing several times), and a scan warning. Kept independent of the rule
/// catalog so the goldens change only when the report writers change.
/// </summary>
internal static class ReportSample
{
    public static AnalysisResult Build()
    {
        RuleMetadata mig1001 = Rule("MIG1001", "Non-SDK-style project file", "Project and build",
            Severity.Medium, EffortBand.Small, ConfidenceTier.Certain,
            "Convert the project to the SDK style.");
        RuleMetadata mig3001 = Rule("MIG3001", "ASP.NET WebForms", "Blocking frameworks",
            Severity.Blocker, EffortBand.Blocker, ConfidenceTier.Certain,
            "Re-architect to Razor Pages, MVC, or Blazor.");
        RuleMetadata mig5001 = Rule("MIG5001", "ConfigurationManager.AppSettings usage", "Configuration",
            Severity.Low, EffortBand.Small, ConfidenceTier.Probable,
            "Add System.Configuration.ConfigurationManager or migrate to Microsoft.Extensions.Configuration.");
        RuleMetadata mig6001 = Rule("MIG6001", "BinaryFormatter", "Serialization and security",
            Severity.Blocker, EffortBand.Large, ConfidenceTier.Probable,
            "Replace with a safe serializer such as System.Text.Json.");
        RuleMetadata mig7001 = Rule("MIG7001", "System.Data.SqlClient", "Data access",
            Severity.Medium, EffortBand.Small, ConfidenceTier.Probable,
            "Switch to Microsoft.Data.SqlClient.");

        DiscoveredProject web = new("Shop.Web/Shop.Web.csproj", "Shop.Web", false, "v4.7.2", [], 2);
        DiscoveredProject core = new("Shop.Core/Shop.Core.csproj", "Shop.Core", true, "net10.0", [], 1);

        // Findings are provided in the analyzer's deterministic order (project, rule, line).
        Finding[] findings =
        [
            new(mig1001, "Project 'Shop.Web' uses the legacy non-SDK project format.",
                "Shop.Web/Shop.Web.csproj", "Shop.Web/Shop.Web.csproj", 2),
            new(mig3001, "Project 'Shop.Web' is an ASP.NET WebForms application (.aspx present).",
                "Shop.Web/Shop.Web.csproj", "Shop.Web/Shop.Web.csproj", 2),
            new(mig5001, "Reads configuration via ConfigurationManager.AppSettings.",
                "Shop.Web/Shop.Web.csproj", "Shop.Web/Default.aspx.cs", 14),
            new(mig5001, "Reads configuration via ConfigurationManager.AppSettings.",
                "Shop.Web/Shop.Web.csproj", "Shop.Web/Global.asax.cs", 20),
            new(mig5001, "Reads configuration via ConfigurationManager.AppSettings.",
                "Shop.Web/Shop.Web.csproj", "Shop.Web/Settings.cs", 8),

            new(mig6001, "Uses BinaryFormatter, which is removed in .NET 9.",
                "Shop.Core/Shop.Core.csproj", "Shop.Core/Cache.cs", 33),
            new(mig7001, "Uses System.Data.SqlClient, which is in maintenance mode.",
                "Shop.Core/Shop.Core.csproj", "Shop.Core/Db.cs", 5),
        ];

        ScanWarning[] warnings =
        [
            new("Skipped 'Shop.Legacy/Shop.Legacy.csproj': project file not found (referenced by the solution but missing on disk).",
                "Shop.Legacy/Shop.Legacy.csproj"),
        ];

        NotAssessedProject[] notAssessed =
        [
            new("Shop.Database", "Shop.Database/Shop.Database.sqlproj", "SQL Server database project",
                "Not a C#/VB project; its contents were not analyzed and must be scoped separately."),
        ];

        return new AnalysisResult("net10.0", [web, core], findings, warnings) { NotAssessed = notAssessed };
    }

    private static RuleMetadata Rule(
        string id, string title, string category,
        Severity severity, EffortBand effort, ConfidenceTier tier, string remediation) =>
        new(id, title, category, severity, effort, tier, remediation,
            $"https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/{id}.md");
}
