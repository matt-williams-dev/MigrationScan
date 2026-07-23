using MigrationScan.Core.Analysis;
using MigrationScan.Core.Models;
using MigrationScan.Core.Rules;

namespace MigrationScan.Core.Tests;

public class SolutionAnalyzerTests
{
    private static SolutionAnalyzer NewAnalyzer() => new(RuleCatalog.LoadDefault());

    [Fact]
    public void LegacySolutionProducesSingleMig1001Finding()
    {
        AnalysisResult result = NewAnalyzer().Analyze(
            Fixtures.Path("LegacyWebForms", "LegacyWebForms.sln"), "net10.0");

        Finding finding = Assert.Single(result.Findings);
        Assert.Equal("MIG1001", finding.Rule.Id);
        Assert.Equal(Severity.Medium, finding.Rule.Severity);
        Assert.Equal(ConfidenceTier.Certain, finding.Rule.Tier);
        Assert.Equal("LegacyWebForms/LegacyWebForms.csproj", finding.ProjectPath);
    }

    [Fact]
    public void CleanModernSolutionProducesNoFindings()
    {
        AnalysisResult result = NewAnalyzer().Analyze(
            Fixtures.Path("ModernClean", "ModernClean.sln"), "net10.0");

        Assert.Empty(result.Findings);
        Assert.Single(result.Projects);
    }

    [Fact]
    public void SingleProjectPathIsSupported()
    {
        AnalysisResult result = NewAnalyzer().Analyze(
            Fixtures.Path("LegacyWebForms", "LegacyWebForms", "LegacyWebForms.csproj"), "net10.0");

        Assert.Single(result.Findings);
    }

    [Fact]
    public void OutputPathsUseForwardSlashesRegardlessOfHostOs()
    {
        AnalysisResult result = NewAnalyzer().Analyze(
            Fixtures.Path("LegacyWebForms", "LegacyWebForms.sln"), "net10.0");

        Assert.DoesNotContain('\\', result.Findings[0].ProjectPath);
    }
}
