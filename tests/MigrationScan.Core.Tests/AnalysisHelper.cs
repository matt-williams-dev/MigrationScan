using MigrationScan.Core.Analysis;
using MigrationScan.Core.Models;

namespace MigrationScan.Core.Tests;

/// <summary>Convenience helpers for running the full analyzer over a fixture.</summary>
internal static class AnalysisHelper
{
    public static AnalysisResult AnalyzeFixture(params string[] fixtureParts) =>
        SolutionAnalyzer.CreateDefault().Analyze(Fixtures.Path(fixtureParts), "net10.0");

    public static IReadOnlySet<string> RuleIds(this AnalysisResult result) =>
        result.Findings.Select(f => f.Rule.Id).ToHashSet(StringComparer.Ordinal);

    public static Finding Finding(this AnalysisResult result, string ruleId) =>
        result.Findings.Single(f => f.Rule.Id == ruleId);
}
