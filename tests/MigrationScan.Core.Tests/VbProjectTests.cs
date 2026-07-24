using MigrationScan.Core.Models;

namespace MigrationScan.Core.Tests;

/// <summary>
/// VB.NET support (Phase 6): <c>.vbproj</c> projects are discovered and scanned for the
/// language-agnostic project/dependency/framework rules. VB source-level (Tier 2) rules are
/// a further step, so no syntax findings are produced for VB source yet.
/// </summary>
public class VbProjectTests
{
    private static readonly AnalysisResult Result =
        AnalysisHelper.AnalyzeFixture("LegacyVbApp", "LegacyVbApp.sln");

    private static readonly IReadOnlySet<string> Rules = Result.RuleIds();

    private static readonly string[] SyntaxRuleIds =
    [
        "MIG3004", "MIG3005", "MIG3010", "MIG4001", "MIG4002", "MIG4004",
        "MIG4008", "MIG5001", "MIG6001", "MIG6004", "MIG7001", "MIG8002", "MIG8003",
    ];

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

    [Fact]
    public void VbSourceIsNotSyntaxAnalyzedYet()
    {
        // Module1.vb calls ConfigurationManager.AppSettings — the C# equivalent would raise
        // MIG5001, but VB source is not parsed yet, so no syntax rule fires.
        Assert.Empty(Rules.Intersect(SyntaxRuleIds));
    }

    [Fact]
    public void DirectVbprojPathIsSupported()
    {
        AnalysisResult result = AnalysisHelper.AnalyzeFixture("LegacyVbApp", "LegacyVbApp", "LegacyVbApp.vbproj");
        Assert.Contains("MIG1001", result.RuleIds());
    }
}
