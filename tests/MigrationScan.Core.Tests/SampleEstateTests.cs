using MigrationScan.Core.Models;

namespace MigrationScan.Core.Tests;

/// <summary>
/// Guards the WoodgroveBanking fixture, which is the estate behind the published sample report.
/// That report is public marketing material, so it going stale is a credibility problem rather
/// than a test failure somebody notices later.
/// </summary>
/// <remarks>
/// These assert the <em>shapes</em> the sample is meant to show, never total counts. A new rule
/// should not break this file; a fixture that quietly stops exercising WebForms, BinaryFormatter,
/// unresearchable third-party references, Windows lock-in, or a non-assessable project should.
/// </remarks>
public class SampleEstateTests
{
    private static readonly AnalysisResult Estate =
        AnalysisHelper.AnalyzeFixture("WoodgroveBanking", "WoodgroveBanking.sln");

    private static IReadOnlySet<string> RuleIds => Estate.RuleIds();

    [Theory]
    [InlineData("MIG3001")] // WebForms — the architectural blocker
    [InlineData("MIG3002")] // System.Web from a class library
    [InlineData("MIG6001")] // BinaryFormatter
    [InlineData("MIG1006")] // COM reference
    [InlineData("MIG1010")] // vendored DLL with no NuGet equivalent
    [InlineData("MIG4002")] // Registry
    [InlineData("MIG4003")] // WMI
    [InlineData("MIG4013")] // P/Invoke to a Windows system library
    public void ExercisesTheShapeTheSampleReportAdvertises(string ruleId) =>
        Assert.Contains(ruleId, RuleIds);

    [Fact]
    public void HasAProjectItCannotAssess()
    {
        NotAssessedProject skipped = Assert.Single(Estate.NotAssessed);
        Assert.EndsWith(".sqlproj", skipped.Path, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void InventoriesThirdPartyReferencesThatNeedResearchRatherThanAnUpgrade()
    {
        // A COM type library and a checked-in vendor assembly: the two kinds with no upstream to
        // look up, which is the point the sample makes about the reference inventory.
        Assert.Contains(Estate.ThirdPartyReferences, r => r.Kind == ReferenceKind.Com);
        Assert.Contains(Estate.ThirdPartyReferences, r => r.Kind == ReferenceKind.VendoredAssembly);
    }

    [Fact]
    public void TheTwoPortabilityStancesDifferByTheWindowsLockIn()
    {
        int crossPlatform = Estate.ForTarget("net10.0").ActiveFindings.Count();
        int windows = Estate.ForTarget("net10.0-windows").ActiveFindings.Count();

        // The gap is the whole argument for reporting both stances, so an estate where they
        // match would make the sample report say nothing.
        Assert.True(
            crossPlatform > windows,
            $"Expected the cross-platform stance to carry more findings than the Windows one, got {crossPlatform} and {windows}.");
        Assert.All(Estate.ForTarget("net10.0-windows").SatisfiedFindings,
            f => Assert.Equal(RulePlatform.Windows, f.Rule.Platform));
    }

    [Fact]
    public void NamesNoRealVendor()
    {
        // The sample is published, so the fixture carries Microsoft-style fictional companies
        // only. Anything real here would reach a marketing page.
        string[] realVendors = ["telerik", "infragistics", "devexpress", "syncfusion", "newtonsoft", "componentone"];

        foreach (ReferenceRecord reference in Estate.References)
        {
            Assert.DoesNotContain(realVendors, v => reference.Name.Contains(v, StringComparison.OrdinalIgnoreCase));
        }
    }
}
