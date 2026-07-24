using MigrationScan.Core.Models;

namespace MigrationScan.Core.Tests;

/// <summary>
/// Coverage for the native-interop / Windows lock-in rules: MIG1006 (COM reference),
/// MIG1010 (vendored DLL), MIG4013 (P/Invoke to a Windows system DLL). These were driven
/// by a real scanner-integration project whose native surface was previously invisible.
/// </summary>
public class NativeInteropRulesTests
{
    private static readonly AnalysisResult Result =
        AnalysisHelper.AnalyzeFixture("NativeInterop", "NativeInterop.sln");

    private static readonly IReadOnlySet<string> Ids = Result.RuleIds();

    private static readonly IReadOnlySet<string> Modern =
        AnalysisHelper.AnalyzeFixture("ModernClean", "ModernClean.sln").RuleIds();

    [Theory]
    [InlineData("MIG1006")] // COMReference
    [InlineData("MIG1010")] // vendored HintPath assembly
    [InlineData("MIG4013")] // DllImport("kernel32.dll") / user32
    public void NativeInteropFixtureTriggers(string ruleId) => Assert.Contains(ruleId, Ids);

    [Theory]
    [InlineData("MIG1006")]
    [InlineData("MIG1010")]
    [InlineData("MIG4013")]
    public void CleanFixtureTriggersNothing(string ruleId) => Assert.DoesNotContain(ruleId, Modern);

    [Fact]
    public void ComReferenceIsCertainAndNamesTheComponent()
    {
        Finding com = Result.Finding("MIG1006");
        Assert.Equal(ConfidenceTier.Certain, com.Rule.Tier);
        Assert.Contains("RANGERLib", com.Message);
    }

    [Fact]
    public void VendoredRuleFlagsTheCheckedInDllButNotTheNuGetRestore()
    {
        // Only AxRANGERLib.dll (a checked-in path) is vendored; the Newtonsoft.Json HintPath
        // points into the packages cache and must not be flagged.
        Finding vendored = Assert.Single(Result.Findings, f => f.Rule.Id == "MIG1010");
        Assert.Contains("AxRANGERLib", vendored.Message);
        Assert.DoesNotContain("Newtonsoft", vendored.Message);
        Assert.Equal(ConfidenceTier.Certain, vendored.Rule.Tier);
    }

    [Fact]
    public void PInvokeRuleFlagsWindowsSystemDllsButNotBespokeNativeLibraries()
    {
        // kernel32 and user32 are Windows system libraries; scanner_sdk.dll is not.
        Finding[] pinvoke = Result.Findings.Where(f => f.Rule.Id == "MIG4013").ToArray();
        Assert.Equal(2, pinvoke.Length);
        Assert.All(pinvoke, f => Assert.Equal(ConfidenceTier.Probable, f.Rule.Tier));
        Assert.Contains(pinvoke, f => f.Message.Contains("kernel32", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(pinvoke, f => f.Message.Contains("user32", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(pinvoke, f => f.Message.Contains("scanner_sdk", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AllThreeAreFramedAsWindowsLockInNotAbsoluteBlockers()
    {
        // None of these are gone-everywhere APIs; each remediation must point at the net-windows path.
        foreach (string ruleId in new[] { "MIG1006", "MIG1010", "MIG4013" })
        {
            Finding finding = Result.Findings.First(f => f.Rule.Id == ruleId);
            Assert.NotEqual(Severity.Blocker, finding.Rule.Severity);
        }
    }
}
