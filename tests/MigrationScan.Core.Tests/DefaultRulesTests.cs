using MigrationScan.Core.Engine;
using MigrationScan.Core.Rules;

namespace MigrationScan.Core.Tests;

public class DefaultRulesTests
{
    [Fact]
    public void EveryShippedRuleHasMetadataInTheCatalog()
    {
        // CreateAll throws from catalog.Get(...) if any rule's metadata is missing.
        IReadOnlyList<IMigrationRule> rules = DefaultRules.CreateAll(
            RuleCatalog.LoadDefault(), PackageCompatibilityCatalog.LoadDefault());

        Assert.NotEmpty(rules);
    }

    [Fact]
    public void RuleIdsAreUnique()
    {
        IReadOnlyList<IMigrationRule> rules = DefaultRules.CreateAll(
            RuleCatalog.LoadDefault(), PackageCompatibilityCatalog.LoadDefault());

        IEnumerable<string> ids = rules.Select(r => r.RuleId);
        Assert.Equal(rules.Count, ids.Distinct().Count());
    }
}
