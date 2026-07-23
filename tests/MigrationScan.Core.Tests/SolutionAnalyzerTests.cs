using MigrationScan.Core.Models;

namespace MigrationScan.Core.Tests;

public class SolutionAnalyzerTests
{
    [Fact]
    public void CleanModernSolutionProducesNoFindings()
    {
        AnalysisResult result = AnalysisHelper.AnalyzeFixture("ModernClean", "ModernClean.sln");

        Assert.Empty(result.Findings);
        Assert.Single(result.Projects);
    }

    [Fact]
    public void FindingsAreSortedDeterministically()
    {
        AnalysisResult first = AnalysisHelper.AnalyzeFixture("LegacyWebForms", "LegacyWebForms.sln");
        AnalysisResult second = AnalysisHelper.AnalyzeFixture("LegacyWebForms", "LegacyWebForms.sln");

        Assert.Equal(
            first.Findings.Select(f => (f.Rule.Id, f.FilePath, f.Line)),
            second.Findings.Select(f => (f.Rule.Id, f.FilePath, f.Line)));
    }

    [Fact]
    public void OutputPathsUseForwardSlashesRegardlessOfHostOs()
    {
        AnalysisResult result = AnalysisHelper.AnalyzeFixture("LegacyWebForms", "LegacyWebForms.sln");

        Assert.All(result.Findings, f =>
        {
            Assert.DoesNotContain('\\', f.ProjectPath);
            Assert.DoesNotContain('\\', f.FilePath ?? string.Empty);
        });
    }

    [Fact]
    public void SingleProjectPathIsSupported()
    {
        AnalysisResult result = AnalysisHelper.AnalyzeFixture(
            "LegacyWebForms", "LegacyWebForms", "LegacyWebForms.csproj");

        Assert.Contains("MIG1001", result.RuleIds());
    }
}
