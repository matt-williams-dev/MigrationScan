using MigrationScan.Core.Discovery;
using MigrationScan.Core.Models;

namespace MigrationScan.Core.Tests;

public class ProjectParserTests
{
    [Fact]
    public void ParsesLegacyProjectAsNonSdkStyle()
    {
        string csproj = Fixtures.Path("LegacyWebForms", "LegacyWebForms", "LegacyWebForms.csproj");

        DiscoveredProject project = ProjectParser.Parse(csproj, "LegacyWebForms/LegacyWebForms.csproj");

        Assert.False(project.IsSdkStyle);
        Assert.Equal("LegacyWebForms", project.Name);
        Assert.Equal("v4.7.2", project.TargetFramework);
        Assert.Equal(2, project.RootElementLine); // <?xml?> on line 1, <Project> on line 2
    }

    [Fact]
    public void ReadsAssemblyReferencesFromLegacyProject()
    {
        string csproj = Fixtures.Path("LegacyWebForms", "LegacyWebForms", "LegacyWebForms.csproj");

        DiscoveredProject project = ProjectParser.Parse(csproj, "LegacyWebForms/LegacyWebForms.csproj");

        Assert.Contains("System.Web", project.References);
        Assert.Contains("System.Configuration", project.References);
    }

    [Fact]
    public void ParsesModernProjectAsSdkStyle()
    {
        string csproj = Fixtures.Path("ModernClean", "ModernClean", "ModernClean.csproj");

        DiscoveredProject project = ProjectParser.Parse(csproj, "ModernClean/ModernClean.csproj");

        Assert.True(project.IsSdkStyle);
        Assert.Equal("net10.0", project.TargetFramework);
        Assert.Equal(1, project.RootElementLine); // <Project Sdk=...> on line 1, no xml declaration
    }
}
