using MigrationScan.Core.Models;

namespace MigrationScan.Core.Tests;

/// <summary>
/// Positive + negative coverage per rule against the fixtures (spec Phase 2 "done when").
/// Each rule fires in a fixture that should trigger it and is absent from fixtures that
/// should not — ModernClean being the universal zero-findings guard.
/// </summary>
public class RulesTests
{
    private static readonly IReadOnlySet<string> WebForms =
        AnalysisHelper.AnalyzeFixture("LegacyWebForms", "LegacyWebForms.sln").RuleIds();

    private static readonly IReadOnlySet<string> Library =
        AnalysisHelper.AnalyzeFixture("LegacyLibrary", "LegacyLibrary.sln").RuleIds();

    private static readonly IReadOnlySet<string> Modern =
        AnalysisHelper.AnalyzeFixture("ModernClean", "ModernClean.sln").RuleIds();

    [Theory]
    [InlineData("MIG1001")] // non-SDK-style
    [InlineData("MIG1002")] // packages.config
    [InlineData("MIG1005")] // Telerik GAC reference
    [InlineData("MIG2001")] // Microsoft.AspNet.Mvc (incompatible package)
    [InlineData("MIG3001")] // WebForms (.aspx present)
    [InlineData("MIG5001")] // ConfigurationManager.AppSettings
    public void WebFormsFixtureTriggers(string ruleId) => Assert.Contains(ruleId, WebForms);

    [Fact]
    public void WebFormsFixtureDoesNotTriggerSystemWebOutsideWebForms() =>
        Assert.DoesNotContain("MIG3002", WebForms); // it *is* WebForms, so MIG3001 owns it

    [Fact]
    public void LibraryFixtureTriggersSystemWebOutsideWebForms() =>
        Assert.Contains("MIG3002", Library);

    [Fact]
    public void LibraryFixtureIsNotWebFormsAndHasNoGacOrPackageFindings()
    {
        Assert.DoesNotContain("MIG3001", Library); // no .aspx
        Assert.DoesNotContain("MIG1005", Library); // System.Web is a framework assembly, not a GAC dep
        Assert.DoesNotContain("MIG2001", Library); // no packages
    }

    [Theory]
    [InlineData("MIG1001")]
    [InlineData("MIG1002")]
    [InlineData("MIG1005")]
    [InlineData("MIG2001")]
    [InlineData("MIG3001")]
    [InlineData("MIG3002")]
    [InlineData("MIG5001")]
    public void CleanFixtureTriggersNothing(string ruleId) => Assert.DoesNotContain(ruleId, Modern);

    [Fact]
    public void Mig2001FlagsTheIncompatiblePackageNotTheCompatibleOne()
    {
        AnalysisResult result = AnalysisHelper.AnalyzeFixture("LegacyWebForms", "LegacyWebForms.sln");

        Finding mig2001 = result.Finding("MIG2001");
        Assert.Contains("Microsoft.AspNet.Mvc", mig2001.Message);
        Assert.EndsWith("packages.config", mig2001.FilePath);
        // Newtonsoft.Json is compatible; only one MIG2001 finding is expected.
        Assert.Single(result.Findings, f => f.Rule.Id == "MIG2001");
    }

    [Fact]
    public void Mig5001IsReportedAsProbableNotCertain()
    {
        AnalysisResult result = AnalysisHelper.AnalyzeFixture("LegacyWebForms", "LegacyWebForms.sln");

        Finding mig5001 = result.Finding("MIG5001");
        Assert.Equal(ConfidenceTier.Probable, mig5001.Rule.Tier);
        Assert.EndsWith("AppConfig.cs", mig5001.FilePath);
    }

    [Fact]
    public void Mig3001IsABlocker()
    {
        AnalysisResult result = AnalysisHelper.AnalyzeFixture("LegacyWebForms", "LegacyWebForms.sln");

        Assert.Equal(Severity.Blocker, result.Finding("MIG3001").Rule.Severity);
    }
}
