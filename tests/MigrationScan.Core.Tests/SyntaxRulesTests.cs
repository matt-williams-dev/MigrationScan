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
        // Added in the later catalog batch:
        "MIG3009", "MIG4003", "MIG4005", "MIG6005", "MIG7003", "MIG7006",
        // WCF client (companion to MIG3004); WcfHost.cs imports System.ServiceModel.
        "MIG3015",
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
    public void CodePageRuleIgnoresUnicodeEncodings()
    {
        AnalysisResult result = AnalysisHelper.AnalyzeFixture("LegacySyntax", "LegacySyntax.sln");

        // Encodings.cs calls GetEncoding(1252), GetEncoding("utf-8"), and GetEncoding(65001).
        // Only the 1252 code page is flagged — the Unicode name and the 65001 number are ignored.
        Assert.Single(result.Findings, f => f.Rule.Id == "MIG8003");
    }

    [Fact]
    public void NamespaceRulesMatchFullyQualifiedReferencesWithoutAUsing()
    {
        AnalysisResult result = AnalysisHelper.AnalyzeFixture("LegacySyntax", "LegacySyntax.sln");

        // QualifiedUsage.cs uses System.Data.SqlClient.SqlConnection with no `using`.
        Assert.Contains(
            result.Findings,
            f => f.Rule.Id == "MIG7001" && (f.FilePath?.EndsWith("QualifiedUsage.cs") ?? false));
    }

    [Fact]
    public void IdenticalFindingsAreDeduplicated()
    {
        AnalysisResult result = AnalysisHelper.AnalyzeFixture("LegacySyntax", "LegacySyntax.sln");

        // RegistryReader.cs uses both Registry and RegistryKey on one line; MIG4002 collapses to one.
        Assert.Single(result.Findings, f => f.Rule.Id == "MIG4002");
    }
}
