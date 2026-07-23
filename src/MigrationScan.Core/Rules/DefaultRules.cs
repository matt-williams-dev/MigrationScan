using MigrationScan.Core.Engine;

namespace MigrationScan.Core.Rules;

/// <summary>
/// Builds the built-in rule set, wiring each rule to its metadata from the catalog.
/// This is the single place that lists which rules ship.
/// </summary>
public static class DefaultRules
{
    public static IReadOnlyList<IMigrationRule> CreateAll(
        RuleCatalog catalog,
        PackageCompatibilityCatalog packageCatalog) =>
    [
        new Mig1001NonSdkProject(catalog.Get(Mig1001NonSdkProject.Id)),
        new Mig1002PackagesConfig(catalog.Get(Mig1002PackagesConfig.Id)),
        new Mig1005GacReference(catalog.Get(Mig1005GacReference.Id)),
        new Mig2001IncompatiblePackage(catalog.Get(Mig2001IncompatiblePackage.Id), packageCatalog),
        new Mig3001WebForms(catalog.Get(Mig3001WebForms.Id)),
        new Mig3002SystemWeb(catalog.Get(Mig3002SystemWeb.Id)),
        new Mig5001ConfigurationManagerAppSettings(catalog.Get(Mig5001ConfigurationManagerAppSettings.Id)),
    ];
}
