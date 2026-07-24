using MigrationScan.Core.Analysis;
using MigrationScan.Core.Effort;
using MigrationScan.Core.Models;

namespace MigrationScan.Core.Tests;

/// <summary>
/// Portability awareness (spec §5): a Windows target framework satisfies Windows lock-in
/// findings, so they are downgraded — kept visible but out of the counts, effort, and
/// <c>--fail-on</c> threshold. Gone-everywhere findings stay active regardless of target.
/// </summary>
public class TargetPlatformTests
{
    [Theory]
    [InlineData("net10.0-windows", true)]
    [InlineData("net8.0-windows", true)]
    [InlineData("net8.0-windows10.0.19041.0", true)]
    [InlineData("NET10.0-WINDOWS", true)] // case-insensitive
    [InlineData("net10.0", false)]
    [InlineData("net8.0", false)]
    [InlineData("netstandard2.0", false)]
    [InlineData("net48", false)]
    public void IsWindowsRecognizesTheWindowsPlatformSuffix(string tfm, bool expected) =>
        Assert.Equal(expected, TargetPlatform.IsWindows(tfm));

    private static AnalysisResult Analyze(string target) =>
        SolutionAnalyzer.CreateDefault().Analyze(
            Fixtures.Path("NativeInterop", "NativeInterop.sln"), target);

    [Fact]
    public void CrossPlatformTargetLeavesWindowsLockInFindingsActive()
    {
        AnalysisResult result = Analyze("net10.0");

        Assert.Empty(result.SatisfiedFindings);
        // MIG1006 (COM) and MIG4013 (P/Invoke) are Windows lock-in — active on a cross-platform target.
        Assert.Contains(result.ActiveFindings, f => f.Rule.Id == "MIG1006");
        Assert.Contains(result.ActiveFindings, f => f.Rule.Id == "MIG4013");
    }

    [Fact]
    public void WindowsTargetSatisfiesWindowsLockInFindingsButNotGoneEverywhereOnes()
    {
        AnalysisResult result = Analyze("net10.0-windows");

        // The Windows lock-in rules are satisfied...
        Assert.Contains(result.SatisfiedFindings, f => f.Rule.Id == "MIG1006");
        Assert.Contains(result.SatisfiedFindings, f => f.Rule.Id == "MIG4013");
        Assert.DoesNotContain(result.ActiveFindings, f => f.Rule.Id == "MIG1006");
        Assert.DoesNotContain(result.ActiveFindings, f => f.Rule.Id == "MIG4013");

        // ...but MIG1010 (vendored DLL) is classified "any" — we can't prove it runs on
        // net-windows, so it stays active regardless of target.
        Assert.Contains(result.ActiveFindings, f => f.Rule.Id == "MIG1010");
        Assert.DoesNotContain(result.SatisfiedFindings, f => f.Rule.Id == "MIG1010");
    }

    [Fact]
    public void SatisfiedFindingsAreExcludedFromCountsAndEffort()
    {
        AnalysisResult cross = Analyze("net10.0");
        AnalysisResult win = Analyze("net10.0-windows");

        // Fewer findings count as cost on the Windows target.
        Assert.True(win.ActiveFindings.Count() < cross.ActiveFindings.Count());

        // And the effort estimate is strictly lower (the satisfied findings carried effort).
        EffortEstimate crossEffort = EffortModel.ForSolution(cross);
        EffortEstimate winEffort = EffortModel.ForSolution(win);
        Assert.True(winEffort.MaxDays < crossEffort.MaxDays);
    }

    [Fact]
    public void SatisfiedFindingsDoNotTripTheFailOnThresholdOrTheCounts()
    {
        // A result whose only finding is a satisfied Windows lock-in finding.
        RuleMetadata comRule = new(
            "MIG1006", "COM reference", "Project and build", Severity.Medium,
            EffortBand.Medium, ConfidenceTier.Certain, "…", "…") { Platform = RulePlatform.Windows };
        Finding satisfied = new(comRule, "COM reference.", "P/P.csproj", "P/P.csproj", 1)
        {
            SatisfiedByTarget = true,
        };
        AnalysisResult result = new("net10.0-windows", [], [satisfied], []);

        Assert.False(result.FailsThreshold(Severity.Medium)); // the only finding is satisfied
        Assert.False(result.FailsThreshold(Severity.Low));
        Assert.Equal(0, result.CountsBySeverity()[Severity.Medium]);
        Assert.Single(result.SatisfiedFindings);
        Assert.Empty(result.ActiveFindings);
    }
}
