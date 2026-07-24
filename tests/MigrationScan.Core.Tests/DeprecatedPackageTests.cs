using MigrationScan.Core.Analysis;
using MigrationScan.Core.Models;
using MigrationScan.Core.Rules;

namespace MigrationScan.Core.Tests;

/// <summary>
/// MIG2002 (deprecated package) is online-only: it reports only what the injected package
/// registry knows. Tests use a fake registry — never the network (spec §12).
/// </summary>
public class DeprecatedPackageTests
{
    private sealed class FakeRegistry(params (string Id, string? Alternate)[] deprecated) : IPackageRegistry
    {
        private readonly Dictionary<string, PackageInfo> _deprecated = deprecated.ToDictionary(
            d => d.Id,
            d => new PackageInfo(d.Id, IsDeprecated: true, "Deprecated.", d.Alternate),
            StringComparer.OrdinalIgnoreCase);

        public PackageInfo? Lookup(string packageId) => _deprecated.GetValueOrDefault(packageId);
    }

    private static AnalysisResult AnalyzeWith(IPackageRegistry registry) =>
        new SolutionAnalyzer(RuleCatalog.LoadDefault(), registry)
            .Analyze(Fixtures.Path("LegacyWebForms", "LegacyWebForms.sln"), "net10.0");

    [Fact]
    public void FlagsADeprecatedPackageWhenTheRegistryReportsIt()
    {
        // LegacyWebForms/packages.config references Telerik.Web.UI.
        AnalysisResult result = AnalyzeWith(new FakeRegistry(("Telerik.Web.UI", "Telerik.UI.for.AspNet.Core")));

        Finding finding = result.Finding("MIG2002");
        Assert.Contains("Telerik.Web.UI", finding.Message);
        Assert.Contains("Telerik.UI.for.AspNet.Core", finding.Message);
    }

    [Fact]
    public void OnlyFlagsPackagesTheRegistryMarksDeprecated()
    {
        // Newtonsoft.Json is also referenced but not marked deprecated here.
        AnalysisResult result = AnalyzeWith(new FakeRegistry(("Telerik.Web.UI", null)));

        Assert.Single(result.Findings, f => f.Rule.Id == "MIG2002");
        Assert.DoesNotContain("Newtonsoft.Json", result.Finding("MIG2002").Message);
    }

    [Fact]
    public void OfflineDefaultProducesNoDeprecationFindings()
    {
        // CreateDefault() uses the empty (offline) registry — no network, no MIG2002.
        AnalysisResult result = SolutionAnalyzer.CreateDefault()
            .Analyze(Fixtures.Path("LegacyWebForms", "LegacyWebForms.sln"), "net10.0");

        Assert.DoesNotContain("MIG2002", result.RuleIds());
    }

    [Fact]
    public void EmptyRegistryKnowsNothing() =>
        Assert.Null(EmptyPackageRegistry.Instance.Lookup("Anything"));
}
