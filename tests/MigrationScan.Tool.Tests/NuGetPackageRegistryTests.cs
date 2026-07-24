using MigrationScan.Core.Analysis;
using MigrationScan.Tool;

namespace MigrationScan.Tool.Tests;

/// <summary>
/// Tests the nuget.org search-response parsing against captured payloads — never the network.
/// </summary>
public class NuGetPackageRegistryTests
{
    [Fact]
    public void ParsesDeprecationWithAlternatePackage()
    {
        const string json = """
        { "data": [ {
            "id": "WindowsAzure.Storage",
            "deprecation": {
                "reasons": ["Legacy"],
                "message": "This package is obsolete.",
                "alternatePackage": { "id": "Azure.Storage.Common", "range": "*" }
            }
        } ] }
        """;

        PackageInfo? info = NuGetPackageRegistry.ParseSearchResponse("WindowsAzure.Storage", json);

        Assert.NotNull(info);
        Assert.True(info.IsDeprecated);
        Assert.Equal("Azure.Storage.Common", info.AlternatePackage);
        Assert.Equal("This package is obsolete.", info.DeprecationMessage);
    }

    [Fact]
    public void ParsesDeprecationWithoutAnAlternate()
    {
        const string json = """
        { "data": [ { "id": "DotNetZip", "deprecation": { "reasons": ["Legacy"] } } ] }
        """;

        PackageInfo? info = NuGetPackageRegistry.ParseSearchResponse("DotNetZip", json);

        Assert.NotNull(info);
        Assert.True(info.IsDeprecated);
        Assert.Null(info.AlternatePackage);
    }

    [Fact]
    public void NonDeprecatedPackageIsReportedNotDeprecated()
    {
        const string json = """{ "data": [ { "id": "Newtonsoft.Json" } ] }""";

        PackageInfo? info = NuGetPackageRegistry.ParseSearchResponse("Newtonsoft.Json", json);

        Assert.NotNull(info);
        Assert.False(info.IsDeprecated);
    }

    [Fact]
    public void EmptyResultsReturnNull()
    {
        PackageInfo? info = NuGetPackageRegistry.ParseSearchResponse("Does.Not.Exist", """{ "data": [] }""");

        Assert.Null(info);
    }

    [Fact]
    public void IgnoresResultsForADifferentPackageId()
    {
        const string json = """{ "data": [ { "id": "SomethingElse", "deprecation": { "reasons": ["Legacy"] } } ] }""";

        PackageInfo? info = NuGetPackageRegistry.ParseSearchResponse("Wanted.Package", json);

        Assert.Null(info);
    }

    [Fact]
    public void MatchesPackageIdCaseInsensitively()
    {
        const string json = """{ "data": [ { "id": "DotNetZip", "deprecation": { "reasons": ["Legacy"] } } ] }""";

        PackageInfo? info = NuGetPackageRegistry.ParseSearchResponse("dotnetzip", json);

        Assert.NotNull(info);
        Assert.True(info.IsDeprecated);
    }
}
