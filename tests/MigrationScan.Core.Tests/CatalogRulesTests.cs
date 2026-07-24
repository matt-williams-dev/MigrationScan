using MigrationScan.Core.Models;
using MigrationScan.Core.Rules;

namespace MigrationScan.Core.Tests;

/// <summary>Coverage for the later catalog-batch project/framework rules (MIG1003, MIG3003).</summary>
public class CatalogRulesTests
{
    private static readonly IReadOnlySet<string> AsmxService =
        AnalysisHelper.AnalyzeFixture("LegacyAsmxService", "LegacyAsmxService.sln").RuleIds();

    [Theory]
    [InlineData("MIG1001")] // non-SDK
    [InlineData("MIG1003")] // TargetFrameworkVersion v4.5 (below 4.6.2)
    [InlineData("MIG3003")] // .asmx present
    public void AsmxFixtureTriggers(string ruleId) => Assert.Contains(ruleId, AsmxService);

    [Theory]
    [InlineData("v4.5", true)]
    [InlineData("v4.6.1", true)]
    [InlineData("v4.6.2", false)]
    [InlineData("v4.7.2", false)]
    [InlineData("v4.8", false)]
    [InlineData("v3.5", true)]
    [InlineData("net45", true)]
    [InlineData("net461", true)]
    [InlineData("net462", false)]
    [InlineData("net472", false)]
    [InlineData("net48", false)]
    [InlineData("net10.0", false)]   // modern .NET, not Framework
    [InlineData("net8.0", false)]
    [InlineData("netstandard2.0", false)]
    [InlineData("netcoreapp3.1", false)]
    public void FrameworkVersionParsingClassifiesOldTargets(string tfm, bool belowMinimum)
    {
        bool parsed = Mig1003OldTargetFramework.TryParseFrameworkVersion(tfm, out Version? version);

        if (belowMinimum)
        {
            Assert.True(parsed);
            Assert.True(version < new Version(4, 6, 2));
        }
        else if (parsed)
        {
            // Parsed as a Framework version, but at or above the minimum.
            Assert.True(version >= new Version(4, 6, 2));
        }
        // Modern monikers simply don't parse as a Framework version (parsed == false).
    }

    [Fact]
    public void ObsoleteCryptoAndDataAccessRulesAreProbable()
    {
        AnalysisResult result = AnalysisHelper.AnalyzeFixture("LegacySyntax", "LegacySyntax.sln");

        foreach (string ruleId in new[] { "MIG3009", "MIG4003", "MIG4005", "MIG6005", "MIG7003", "MIG7006" })
        {
            Assert.Equal(ConfidenceTier.Probable, result.Finding(ruleId).Rule.Tier);
        }
    }
}
