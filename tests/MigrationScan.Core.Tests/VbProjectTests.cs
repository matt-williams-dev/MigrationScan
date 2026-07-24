using MigrationScan.Core.Models;

namespace MigrationScan.Core.Tests;

/// <summary>
/// VB.NET support (Phase 6): <c>.vbproj</c> projects are discovered and scanned for both the
/// project-level rules and the Tier 2 syntax rules (the syntax queries are language-neutral,
/// so C# and VB source are both analyzed).
/// </summary>
public class VbProjectTests
{
    private static readonly AnalysisResult Result =
        AnalysisHelper.AnalyzeFixture("LegacyVbApp", "LegacyVbApp.sln");

    private static readonly IReadOnlySet<string> Rules = Result.RuleIds();

    [Fact]
    public void VbProjectIsDiscovered()
    {
        Assert.Single(Result.Projects);
        Assert.EndsWith(".vbproj", Result.Projects[0].Path);
    }

    [Theory]
    [InlineData("MIG1001")] // non-SDK
    [InlineData("MIG1002")] // packages.config
    [InlineData("MIG2001")] // Microsoft.AspNet.Mvc
    [InlineData("MIG3002")] // System.Web outside WebForms
    public void ProjectLevelRulesApplyToVbProjects(string ruleId) => Assert.Contains(ruleId, Rules);

    [Theory]
    [InlineData("MIG5001")] // ConfigurationManager.AppSettings
    [InlineData("MIG4002")] // Registry
    [InlineData("MIG8002")] // Encoding.Default
    [InlineData("MIG8003")] // Encoding.GetEncoding(1252)
    [InlineData("MIG6001")] // BinaryFormatter
    public void SyntaxRulesApplyToVbSource(string ruleId) => Assert.Contains(ruleId, Rules);

    [Fact]
    public void VbSyntaxFindingsAnchorToTheVbFile()
    {
        Finding mig5001 = Result.Finding("MIG5001");
        Assert.EndsWith("Module1.vb", mig5001.FilePath);
        Assert.Equal(ConfidenceTier.Probable, mig5001.Rule.Tier);
    }

    [Fact]
    public void DirectVbprojPathIsSupported()
    {
        AnalysisResult result = AnalysisHelper.AnalyzeFixture("LegacyVbApp", "LegacyVbApp", "LegacyVbApp.vbproj");
        Assert.Contains("MIG1001", result.RuleIds());
        Assert.Contains("MIG5001", result.RuleIds());
    }
}
