using MigrationScan.Core.Models;

namespace MigrationScan.Core.Tests;

public class FailThresholdTests
{
    [Theory]
    [InlineData(Severity.Blocker, Severity.Blocker, true)]
    [InlineData(Severity.High, Severity.Blocker, false)]  // a High finding does NOT fail --fail-on blocker
    [InlineData(Severity.Blocker, Severity.High, true)]   // a Blocker finding fails --fail-on high
    [InlineData(Severity.Medium, Severity.High, false)]   // a Medium finding does not fail --fail-on high
    [InlineData(Severity.Low, Severity.Low, true)]
    [InlineData(Severity.Medium, Severity.Low, true)]     // any finding fails --fail-on low
    public void FailsThresholdComparesSeverity(Severity findingSeverity, Severity threshold, bool expected)
    {
        AnalysisResult result = ResultWith(findingSeverity);
        Assert.Equal(expected, result.FailsThreshold(threshold));
    }

    [Fact]
    public void EmptyResultNeverFails()
    {
        AnalysisResult result = new("net10.0", [], [], []);
        Assert.False(result.FailsThreshold(Severity.Low));
    }

    private static AnalysisResult ResultWith(Severity severity)
    {
        RuleMetadata rule = new("MIG0001", "t", "c", severity, EffortBand.Small,
            ConfidenceTier.Certain, "r", "https://example.test");
        Finding finding = new(rule, "m", "P/P.csproj", "P/F.cs", 1);
        return new AnalysisResult("net10.0", [], [finding], []);
    }
}
