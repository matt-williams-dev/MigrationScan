using MigrationScan.Core.Analysis;
using MigrationScan.Core.Models;
using MigrationScan.Core.Rules;

namespace MigrationScan.Core.Tests;

/// <summary>
/// A solution that references a project which no longer exists on disk must not crash the
/// scan (see PR #1 review). The missing project is skipped with a warning and the rest of
/// the solution is still analyzed.
/// </summary>
public class StaleProjectReferenceTests
{
    private static AnalysisResult Analyze() =>
        new SolutionAnalyzer(RuleCatalog.LoadDefault())
            .Analyze(Fixtures.Path("StaleReference", "StaleReference.sln"), "net10.0");

    [Fact]
    public void MissingProjectProducesAWarningNotACrash()
    {
        AnalysisResult result = Analyze();

        ScanWarning warning = Assert.Single(result.Warnings);
        Assert.Contains("Deleted", warning.Path);
        Assert.Contains("not found", warning.Message);
    }

    [Fact]
    public void PresentProjectIsStillScanned()
    {
        AnalysisResult result = Analyze();

        // The present project is a clean SDK-style project: discovered, no findings.
        Assert.Single(result.Projects);
        Assert.Equal("Present/Present.csproj", result.Projects[0].Path);
    }
}
