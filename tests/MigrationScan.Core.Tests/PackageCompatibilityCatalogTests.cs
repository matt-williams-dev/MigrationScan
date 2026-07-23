using MigrationScan.Core.Rules;

namespace MigrationScan.Core.Tests;

public class PackageCompatibilityCatalogTests
{
    private static readonly PackageCompatibilityCatalog Catalog = PackageCompatibilityCatalog.LoadDefault();

    [Fact]
    public void FindsAKnownIncompatiblePackage()
    {
        IncompatiblePackage? package = Catalog.Find("Microsoft.AspNet.Mvc");

        Assert.NotNull(package);
        Assert.False(string.IsNullOrWhiteSpace(package.Reason));
        Assert.False(string.IsNullOrWhiteSpace(package.Replacement));
    }

    [Fact]
    public void MatchesPackageIdCaseInsensitively() =>
        Assert.NotNull(Catalog.Find("microsoft.aspnet.mvc"));

    [Fact]
    public void ReturnsNullForACompatiblePackage() =>
        Assert.Null(Catalog.Find("Newtonsoft.Json"));
}
