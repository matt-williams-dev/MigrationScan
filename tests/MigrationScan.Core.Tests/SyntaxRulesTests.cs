using MigrationScan.Core.Models;

namespace MigrationScan.Core.Tests;

/// <summary>
/// Positive + negative coverage for the Tier 2 syntax rules (spec Phase 3). Each rule
/// fires on the LegacySyntax fixture and is absent from the clean fixture.
/// </summary>
public class SyntaxRulesTests
{
    private static readonly IReadOnlySet<string> Legacy =
        AnalysisHelper.AnalyzeFixture("LegacySyntax", "LegacySyntax.sln").RuleIds();

    private static readonly IReadOnlySet<string> Modern =
        AnalysisHelper.AnalyzeFixture("ModernClean", "ModernClean.sln").RuleIds();

    public static TheoryData<string> SyntaxRuleIds =>
    [
        "MIG3004", "MIG3005", "MIG3010", "MIG4001", "MIG4002", "MIG4004",
        "MIG4008", "MIG6001", "MIG6004", "MIG7001", "MIG8002", "MIG8003",
    ];

    [Theory]
    [MemberData(nameof(SyntaxRuleIds))]
    public void LegacySyntaxFixtureTriggers(string ruleId) => Assert.Contains(ruleId, Legacy);

    [Theory]
    [MemberData(nameof(SyntaxRuleIds))]
    public void CleanFixtureTriggersNothing(string ruleId) => Assert.DoesNotContain(ruleId, Modern);

    [Fact]
    public void EveryFindingCarriesProbableTier()
    {
        AnalysisResult result = AnalysisHelper.AnalyzeFixture("LegacySyntax", "LegacySyntax.sln");

        // These are all syntax-matched rules; none may claim certainty (spec §5).
        Assert.All(result.Findings, f => Assert.Equal(ConfidenceTier.Probable, f.Rule.Tier));
    }

    [Fact]
    public void CodePageRuleIgnoresUnicodeEncodingNames()
    {
        AnalysisResult result = AnalysisHelper.AnalyzeFixture("LegacySyntax", "LegacySyntax.sln");

        // Encodings.cs calls GetEncoding(1252) and GetEncoding("utf-8"); only the code page is flagged.
        Assert.Single(result.Findings, f => f.Rule.Id == "MIG8003");
    }

    [Fact]
    public void IdenticalFindingsAreDeduplicated()
    {
        AnalysisResult result = AnalysisHelper.AnalyzeFixture("LegacySyntax", "LegacySyntax.sln");

        // RegistryReader.cs uses both Registry and RegistryKey on one line; MIG4002 collapses to one.
        Assert.Single(result.Findings, f => f.Rule.Id == "MIG4002");
    }
}
