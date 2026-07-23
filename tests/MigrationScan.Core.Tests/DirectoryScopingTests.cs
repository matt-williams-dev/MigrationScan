using MigrationScan.Core.Models;

namespace MigrationScan.Core.Tests;

/// <summary>
/// Regression tests for PR #2 review: a project's analysis must not absorb files from
/// nested projects or hidden folders, and an empty &lt;HintPath&gt; must not suppress MIG1005.
/// </summary>
public class DirectoryScopingTests
{
    [Fact]
    public void NestedProjectAndHiddenFolderFilesAreNotAbsorbed()
    {
        // Outer is a clean SDK project. Its subtree contains a nested project (Inner, with
        // an .aspx and a ConfigurationManager.AppSettings usage) and a .hidden folder (also
        // with a ConfigurationManager usage). None of those belong to Outer.
        AnalysisResult result = AnalysisHelper.AnalyzeFixture("NestedProjects", "NestedProjects.sln");

        Assert.Empty(result.Findings); // no MIG3001 from Inner/Page.aspx, no MIG5001 from Inner or .hidden
    }

    [Fact]
    public void EmptyHintPathDoesNotSuppressGacReferenceFinding()
    {
        AnalysisResult result = AnalysisHelper.AnalyzeFixture("GacHintPath", "GacHintPath.csproj");

        // Only the empty-HintPath reference is flagged; the one with a real HintPath is not.
        Finding finding = Assert.Single(result.Findings, f => f.Rule.Id == "MIG1005");
        Assert.Contains("Contoso.Legacy", finding.Message);
        Assert.DoesNotContain("Contoso.Vendored", finding.Message);
    }
}
