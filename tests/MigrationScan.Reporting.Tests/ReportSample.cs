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

        DiscoveredProject web = new("Shop.Web/Shop.Web.csproj", "Shop.Web", false, "v4.7.2", 2);
        DiscoveredProject core = new("Shop.Core/Shop.Core.csproj", "Shop.Core", true, "net10.0", 1);

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

        // Every reference kind, plus a package used by both projects (one row, "2 projects"),
        // a framework assembly (excluded from the table but counted in the note), and a
        // project reference (its own section).
        ReferenceRecord[] references =
        [
            new(ReferenceKind.Package, "Newtonsoft.Json", "13.0.3", null, false,
                "Shop.Core/Shop.Core.csproj", "Shop.Core/Shop.Core.csproj", 11),
            new(ReferenceKind.Package, "Newtonsoft.Json", "13.0.3", null, false,
                "Shop.Web/Shop.Web.csproj", "Shop.Web/packages.config", 4),
            new(ReferenceKind.Assembly, "System.Web", null, null, true,
                "Shop.Web/Shop.Web.csproj", "Shop.Web/Shop.Web.csproj", 8),
            new(ReferenceKind.Assembly, "Telerik.Web.UI", "2015.3.930.45", null, false,
                "Shop.Web/Shop.Web.csproj", "Shop.Web/Shop.Web.csproj", 9),
            new(ReferenceKind.VendoredAssembly, "Contoso.Payments", "2.1.0.0", "libs/Contoso.Payments.dll", false,
                "Shop.Web/Shop.Web.csproj", "Shop.Web/Shop.Web.csproj", 12),
            new(ReferenceKind.Com, "MSXML2", "6.0", "{f5078f18-c551-11d3-89b9-0000f81fe221}", false,
                "Shop.Web/Shop.Web.csproj", "Shop.Web/Shop.Web.csproj", 18),
            new(ReferenceKind.WebService, "PricingService", null, "http://pricing.internal/Pricing.asmx", false,
                "Shop.Web/Shop.Web.csproj", "Shop.Web/Shop.Web.csproj", 24),
            new(ReferenceKind.Project, "Shop.Core", null, "Shop.Core/Shop.Core.csproj", false,
                "Shop.Web/Shop.Web.csproj", "Shop.Web/Shop.Web.csproj", 30),
        ];

        return new AnalysisResult("net10.0", [web, core], findings, warnings)
        {
            NotAssessed = notAssessed,
            References = references,
        };
    }

    /// <summary>
    /// A result scanned with a Windows target (<c>net10.0-windows</c>) that mixes gone-everywhere
    /// findings (still active) with Windows lock-in findings the target satisfies (downgraded).
    /// Exercises the "satisfied by target" report sections and the active-only counts/effort.
    /// </summary>
    public static AnalysisResult BuildWindowsTarget()
    {
        RuleMetadata mig3001 = Rule("MIG3001", "ASP.NET WebForms", "Blocking frameworks",
            Severity.Blocker, EffortBand.Blocker, ConfidenceTier.Certain,
            "Re-architect to Razor Pages, MVC, or Blazor.");
        RuleMetadata mig7001 = Rule("MIG7001", "System.Data.SqlClient", "Data access",
            Severity.Medium, EffortBand.Small, ConfidenceTier.Probable,
            "Switch to Microsoft.Data.SqlClient.");
        // Windows lock-in rules (platform = windows).
        RuleMetadata mig4002 = Rule("MIG4002", "Windows Registry access", "Runtime failures",
            Severity.High, EffortBand.Small, ConfidenceTier.Probable,
            "Move registry state to a cross-platform store; on Windows-only deployments it is fine.")
            with { Platform = RulePlatform.Windows };
        RuleMetadata mig1006 = Rule("MIG1006", "COM reference or interop assembly", "Project and build",
            Severity.Medium, EffortBand.Medium, ConfidenceTier.Certain,
            "Supported on net-windows; replace only if going cross-platform.")
            with { Platform = RulePlatform.Windows };

        DiscoveredProject app = new("Scan.App/Scan.App.csproj", "Scan.App", false, "v4.7.2", 2);

        Finding[] findings =
        [
            new(mig1006, "Project 'Scan.App' has a COM reference ('RANGERLib').",
                "Scan.App/Scan.App.csproj", "Scan.App/Scan.App.csproj", 12) { SatisfiedByTarget = true },
            new(mig3001, "Project 'Scan.App' is an ASP.NET WebForms application (.aspx present).",
                "Scan.App/Scan.App.csproj", "Scan.App/Scan.App.csproj", 2),
            new(mig4002, "Uses Microsoft.Win32.Registry.",
                "Scan.App/Scan.App.csproj", "Scan.App/Settings.cs", 8) { SatisfiedByTarget = true },
            new(mig4002, "Uses Microsoft.Win32.Registry.",
                "Scan.App/Scan.App.csproj", "Scan.App/Startup.cs", 19) { SatisfiedByTarget = true },
            new(mig7001, "Uses System.Data.SqlClient, which is in maintenance mode.",
                "Scan.App/Scan.App.csproj", "Scan.App/Db.cs", 5),
        ];

        return new AnalysisResult("net10.0-windows", [app], findings, [])
        {
            References =
            [
                new ReferenceRecord(ReferenceKind.Com, "AxInterop.RANGERLib", "1.0.0.0",
                    "libs/AxInterop.RANGERLib.dll", false,
                    "Scan.App/Scan.App.csproj", "Scan.App/Scan.App.csproj", 14),
                new ReferenceRecord(ReferenceKind.Com, "RANGERLib", "1.0",
                    "{8b3a1e60-0000-0000-0000-000000000001}", false,
                    "Scan.App/Scan.App.csproj", "Scan.App/Scan.App.csproj", 12),
            ],
        };
    }

    private static RuleMetadata Rule(
        string id, string title, string category,
        Severity severity, EffortBand effort, ConfidenceTier tier, string remediation) =>
        new(id, title, category, severity, effort, tier, remediation,
            $"https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/{id}.md");
}
