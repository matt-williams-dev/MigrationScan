using MigrationScan.Core.Discovery;
using MigrationScan.Core.Models;
using MigrationScan.Core.Rules;

namespace MigrationScan.Core.Analysis;

/// <summary>
/// Handles the non-C#/VB projects a solution references. Recognized legacy project types
/// (SSRS, SSIS, setup, Silverlight, web site) become MIG1007 findings; anything else the scan
/// can't analyze (e.g. a SQL or deployment project) becomes a warning, so the user knows it
/// was not assessed rather than silently assuming the solution is fully covered.
/// </summary>
public static class SolutionProjectAnalyzer
{
    // Legacy project types with no modern .NET equivalent, keyed by file extension...
    private static readonly IReadOnlyDictionary<string, string> LegacyByExtension =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".rptproj"] = "an SSRS (SQL Server Reporting Services) report project",
            [".dtproj"] = "an SSIS (SQL Server Integration Services) project",
            [".vdproj"] = "a Visual Studio setup/installer project",
        };

    // ...and by solution project-type GUID (for types without a distinctive extension).
    private static readonly IReadOnlyDictionary<string, string> LegacyByTypeGuid =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["A1591282-1198-4647-A2B1-27E5FF5F6F3B"] = "a Silverlight project",
            ["E24C65DC-7377-472B-9ABA-BC803B73C61A"] = "an ASP.NET Web Site project",
        };

    public static (IReadOnlyList<Finding> Findings, IReadOnlyList<ScanWarning> Warnings) Analyze(
        IEnumerable<SolutionProjectEntry> otherProjects, string rootDirectory, RuleCatalog catalog)
    {
        RuleMetadata mig1007 = catalog.Get(Mig1007LegacyProjectType.Id);
        List<Finding> findings = [];
        List<ScanWarning> warnings = [];

        foreach (SolutionProjectEntry project in otherProjects)
        {
            string relativePath = PathUtilities.ToRelative(rootDirectory, project.AbsolutePath);

            if (LegacyDescription(project) is { } description)
            {
                findings.Add(new Finding(
                    mig1007,
                    $"Project '{project.Name}' is {description}, a legacy project type with no modern .NET equivalent.",
                    relativePath,
                    relativePath,
                    Line: null));
            }
            else
            {
                warnings.Add(new ScanWarning(
                    $"Skipped '{relativePath}': not a C#/VB project ({project.Extension}); its contents were not assessed.",
                    relativePath));
            }
        }

        return (findings, warnings);
    }

    private static string? LegacyDescription(SolutionProjectEntry project)
    {
        if (LegacyByExtension.TryGetValue(project.Extension, out string? byExtension))
        {
            return byExtension;
        }

        string guid = project.TypeGuid.Trim('{', '}');
        return LegacyByTypeGuid.GetValueOrDefault(guid);
    }
}
