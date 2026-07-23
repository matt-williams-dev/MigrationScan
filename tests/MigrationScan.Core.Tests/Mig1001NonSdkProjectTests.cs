using MigrationScan.Core.Models;
using MigrationScan.Core.Rules;

namespace MigrationScan.Core.Tests;

public class Mig1001NonSdkProjectTests
{
    private static readonly RuleMetadata Metadata = RuleCatalog.LoadDefault().Get("MIG1001");

    [Fact]
    public void FlagsNonSdkStyleProject()
    {
        DiscoveredProject project = Project(isSdkStyle: false);

        Finding? finding = Mig1001NonSdkProject.Evaluate(project, Metadata);

        Assert.NotNull(finding);
        Assert.Equal("MIG1001", finding.Rule.Id);
        Assert.Equal("Legacy/Legacy.csproj", finding.FilePath);
        Assert.Equal(2, finding.Line);
    }

    [Fact]
    public void IgnoresSdkStyleProject()
    {
        DiscoveredProject project = Project(isSdkStyle: true);

        Finding? finding = Mig1001NonSdkProject.Evaluate(project, Metadata);

        Assert.Null(finding);
    }

    private static DiscoveredProject Project(bool isSdkStyle) => new(
        Path: "Legacy/Legacy.csproj",
        Name: "Legacy",
        IsSdkStyle: isSdkStyle,
        TargetFramework: isSdkStyle ? "net10.0" : "v4.7.2",
        References: [],
        RootElementLine: 2);
}
