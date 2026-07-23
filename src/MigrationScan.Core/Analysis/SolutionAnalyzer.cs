using System.Xml;
using MigrationScan.Core.Discovery;
using MigrationScan.Core.Models;
using MigrationScan.Core.Rules;

namespace MigrationScan.Core.Analysis;

/// <summary>
/// Runs the analysis: resolve the scan target, parse each project, apply the rules,
/// and return findings in deterministic order.
///
/// A project that is missing or unparseable is skipped with a warning rather than
/// failing the whole scan — large legacy solutions routinely carry stale project
/// references, and one broken project should not abort the assessment.
///
/// Phase 1 walking skeleton — only MIG1001 is wired in. The pluggable rule engine
/// (spec Phase 2) replaces the direct rule call here.
/// </summary>
public sealed class SolutionAnalyzer
{
    private readonly RuleCatalog _catalog;

    public SolutionAnalyzer(RuleCatalog catalog) => _catalog = catalog;

    public AnalysisResult Analyze(string path, string targetFramework)
    {
        ScanInput input = ScanInput.Resolve(path);
        RuleMetadata mig1001 = _catalog.Get(Mig1001NonSdkProject.RuleId);

        List<DiscoveredProject> projects = [];
        List<Finding> findings = [];
        List<ScanWarning> warnings = [];

        foreach (string projectFile in input.ProjectFiles)
        {
            string relativePath = PathUtilities.ToRelative(input.RootDirectory, projectFile);

            DiscoveredProject project;
            try
            {
                project = ProjectParser.Parse(projectFile, relativePath);
            }
            catch (Exception ex) when (IsRecoverable(ex))
            {
                warnings.Add(new ScanWarning($"Skipped '{relativePath}': {Describe(ex)}", relativePath));
                continue;
            }

            projects.Add(project);

            if (Mig1001NonSdkProject.Evaluate(project, mig1001) is { } finding)
            {
                findings.Add(finding);
            }
        }

        return new AnalysisResult(targetFramework, projects, Sort(findings), SortWarnings(warnings));
    }

    // A broken individual project is recoverable — skip it and warn. Anything else
    // (out of memory, access denied at the root, …) is left to propagate as an error.
    private static bool IsRecoverable(Exception ex) =>
        ex is FileNotFoundException
            or DirectoryNotFoundException
            or XmlException
            or InvalidDataException;

    private static string Describe(Exception ex) => ex switch
    {
        FileNotFoundException => "project file not found (referenced by the solution but missing on disk).",
        DirectoryNotFoundException => "project directory not found.",
        XmlException xml => $"malformed project XML ({xml.Message}).",
        InvalidDataException data => data.Message,
        _ => ex.Message,
    };

    // Stable ordering so the same input always produces byte-identical output.
    private static IReadOnlyList<Finding> Sort(IEnumerable<Finding> findings) =>
        findings
            .OrderBy(f => f.ProjectPath, StringComparer.Ordinal)
            .ThenBy(f => f.Rule.Id, StringComparer.Ordinal)
            .ThenBy(f => f.Line ?? 0)
            .ThenBy(f => f.Message, StringComparer.Ordinal)
            .ToList();

    private static IReadOnlyList<ScanWarning> SortWarnings(IEnumerable<ScanWarning> warnings) =>
        warnings
            .OrderBy(w => w.Path, StringComparer.Ordinal)
            .ThenBy(w => w.Message, StringComparer.Ordinal)
            .ToList();
}
