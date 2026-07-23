using MigrationScan.Core.Discovery;
using MigrationScan.Core.Models;
using MigrationScan.Core.Rules;

namespace MigrationScan.Core.Analysis;

/// <summary>
/// Runs the analysis: resolve the scan target, parse each project, apply the rules,
/// and return findings in deterministic order.
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

        foreach (string projectFile in input.ProjectFiles)
        {
            string relativePath = PathUtilities.ToRelative(input.RootDirectory, projectFile);
            DiscoveredProject project = ProjectParser.Parse(projectFile, relativePath);
            projects.Add(project);

            if (Mig1001NonSdkProject.Evaluate(project, mig1001) is { } finding)
            {
                findings.Add(finding);
            }
        }

        return new AnalysisResult(targetFramework, projects, Sort(findings));
    }

    // Stable ordering so the same input always produces byte-identical output.
    private static IReadOnlyList<Finding> Sort(IEnumerable<Finding> findings) =>
        findings
            .OrderBy(f => f.ProjectPath, StringComparer.Ordinal)
            .ThenBy(f => f.Rule.Id, StringComparer.Ordinal)
            .ThenBy(f => f.Line ?? 0)
            .ThenBy(f => f.Message, StringComparer.Ordinal)
            .ToList();
}
