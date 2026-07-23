using MigrationScan.Core.Models;
using MigrationScan.Core.Rules;

namespace MigrationScan.Core.Tests;

public class RuleCatalogTests
{
    [Fact]
    public void LoadsMig1001WithExpectedMetadata()
    {
        RuleMetadata rule = RuleCatalog.LoadDefault().Get("MIG1001");

        Assert.Equal("Non-SDK-style project file", rule.Title);
        Assert.Equal("Project and build", rule.Category);
        Assert.Equal(Severity.Medium, rule.Severity);
        Assert.Equal(EffortBand.Small, rule.Effort);
        Assert.Equal(ConfidenceTier.Certain, rule.Tier);
        Assert.False(string.IsNullOrWhiteSpace(rule.Remediation));
        Assert.StartsWith("https://", rule.DocsUrl);
    }

    [Fact]
    public void UnknownRuleIdThrows()
    {
        RuleCatalog catalog = RuleCatalog.LoadDefault();

        Assert.Throws<KeyNotFoundException>(() => catalog.Get("MIG9999"));
    }
}
