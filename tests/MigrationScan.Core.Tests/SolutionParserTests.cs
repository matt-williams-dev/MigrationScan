using MigrationScan.Core.Discovery;

namespace MigrationScan.Core.Tests;

public class SolutionParserTests
{
    [Fact]
    public void ExtractsProjectPathFromClassicSolution()
    {
        string solution = Fixtures.Path("LegacyWebForms", "LegacyWebForms.sln");

        IReadOnlyList<string> projects = SolutionParser.GetProjectPaths(solution);

        string project = Assert.Single(projects);
        Assert.EndsWith("LegacyWebForms.csproj", project);
        Assert.True(File.Exists(project), $"Resolved project path should exist: {project}");
    }

    [Fact]
    public void ResolvesProjectPathRelativeToSolutionDirectory()
    {
        string solution = Fixtures.Path("ModernClean", "ModernClean.sln");

        string project = SolutionParser.GetProjectPaths(solution).Single();

        Assert.True(Path.IsPathFullyQualified(project));
        Assert.True(File.Exists(project));
    }
}
