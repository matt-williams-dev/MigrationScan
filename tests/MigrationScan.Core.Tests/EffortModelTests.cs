using MigrationScan.Core.Effort;
using MigrationScan.Core.Models;

namespace MigrationScan.Core.Tests;

public class EffortModelTests
{
    [Theory]
    [InlineData(1, 1.0)]
    [InlineData(5, 1.5)]
    [InlineData(6, 2.0)]
    [InlineData(20, 2.0)]
    [InlineData(21, 3.0)]
    [InlineData(100, 3.0)]
    public void OccurrenceFactorFlattens(int occurrences, double expected) =>
        Assert.Equal(expected, EffortModel.OccurrenceFactor(occurrences));

    [Fact]
    public void SingleSmallFindingMapsToItsBandRange()
    {
        EffortEstimate estimate = EffortModel.ForFindings([FindingWith("MIG0001", EffortBand.Small)]);

        Assert.Equal(0.5, estimate.MinDays);
        Assert.Equal(2.0, estimate.MaxDays);
        Assert.Equal(0, estimate.BlockerCount);
    }

    [Fact]
    public void RepeatedOccurrencesOfOneRuleApplyTheFactorNotLinearScaling()
    {
        // 3 occurrences of one Small rule → factor 1.5, not ×3.
        Finding[] findings =
        [
            FindingWith("MIG0001", EffortBand.Small),
            FindingWith("MIG0001", EffortBand.Small),
            FindingWith("MIG0001", EffortBand.Small),
        ];

        EffortEstimate estimate = EffortModel.ForFindings(findings);

        Assert.Equal(0.75, estimate.MinDays); // 0.5 * 1.5
        Assert.Equal(3.0, estimate.MaxDays);  // 2.0 * 1.5
    }

    [Fact]
    public void BlockerBandIsCountedNotEstimatedInDays()
    {
        Finding[] findings =
        [
            FindingWith("MIG3001", EffortBand.Blocker),
            FindingWith("MIG3001", EffortBand.Blocker), // same rule → still one blocking issue
            FindingWith("MIG6001", EffortBand.Blocker),
        ];

        EffortEstimate estimate = EffortModel.ForFindings(findings);

        Assert.Equal(0, estimate.MinDays);
        Assert.Equal(0, estimate.MaxDays);
        Assert.Equal(2, estimate.BlockerCount); // two distinct blocking rules
    }

    [Fact]
    public void DistinctRulesSumTogether()
    {
        Finding[] findings =
        [
            FindingWith("MIG0001", EffortBand.Small),  // 0.5–2.0
            FindingWith("MIG0002", EffortBand.Medium), // 2.0–5.0
        ];

        EffortEstimate estimate = EffortModel.ForFindings(findings);

        Assert.Equal(2.5, estimate.MinDays);
        Assert.Equal(7.0, estimate.MaxDays);
    }

    private static Finding FindingWith(string ruleId, EffortBand effort)
    {
        RuleMetadata rule = new(
            Id: ruleId,
            Title: ruleId,
            Category: "Test",
            Severity: Severity.Medium,
            Effort: effort,
            Tier: ConfidenceTier.Certain,
            Remediation: "n/a",
            DocsUrl: "https://example.test");

        return new Finding(rule, "message", "Proj/Proj.csproj", "Proj/File.cs", 1);
    }
}
