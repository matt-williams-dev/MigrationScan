using MigrationScan.Core.Analysis;
using MigrationScan.Core.Models;

namespace MigrationScan.Core.Tests;

/// <summary>
/// Directory mode scans a whole estate in one pass. The governing rule is that projects are the
/// unit of truth and solutions are the grouping: a project is assessed because it exists, not
/// because a solution claims it. These pin the consequences of that.
/// </summary>
public class EstateDiscoveryTests
{
    private static ScanInput Estate() => ScanInput.Resolve(Fixtures.Path("Estate"));

    private static string Relative(string root, string path) =>
        Path.GetRelativePath(root, path).Replace('\\', '/');

    [Fact]
    public void FindsEverySolutionUnderTheRoot()
    {
        ScanInput input = Estate();

        Assert.Equal(
            ["Alpha.sln", "Beta.sln"],
            input.Solutions.Select(s => Relative(input.RootDirectory, s)));
    }

    [Fact]
    public void ScansAProjectSharedByTwoSolutionsExactlyOnce()
    {
        ScanInput input = Estate();

        // Shared is referenced by both Alpha.sln and Beta.sln. Counting it twice would double
        // its findings and its effort, which is the whole risk of merging solutions into one scan.
        Assert.Single(input.ProjectFiles, p => Relative(input.RootDirectory, p) == "Shared/Shared.csproj");
    }

    [Fact]
    public void AssessesAProjectNoSolutionReferences()
    {
        ScanInput input = Estate();

        Assert.Contains("Orphan/Orphan.csproj", input.ProjectFiles.Select(p => Relative(input.RootDirectory, p)));
        Assert.Equal(
            ["Orphan/Orphan.csproj"],
            input.OrphanProjects.Select(p => Relative(input.RootDirectory, p)));
    }

    [Fact]
    public void DoesNotTreatSolutionReferencedProjectsAsOrphans()
    {
        ScanInput input = Estate();

        IEnumerable<string> orphans = input.OrphanProjects.Select(p => Relative(input.RootDirectory, p));
        Assert.DoesNotContain("Alpha/Alpha.csproj", orphans);
        Assert.DoesNotContain("Shared/Shared.csproj", orphans);
    }

    [Fact]
    public void SkipsBuildOutputAndRestoredPackages()
    {
        ScanInput input = Estate();
        IEnumerable<string> found = input.ProjectFiles.Select(p => Relative(input.RootDirectory, p));

        // Scanning either of these would attribute build artifacts or a vendor's source to the
        // customer's estate, inflating both the finding count and the quote.
        Assert.DoesNotContain("Alpha/bin/Debug/Ignored.csproj", found);
        Assert.DoesNotContain("packages/Vendor/Vendor.csproj", found);
    }

    [Fact]
    public void CollectsNonCSharpProjectsFromDiskAndFromSolutions()
    {
        ScanInput input = Estate();
        IEnumerable<string> others = input.OtherProjects.Select(p => Relative(input.RootDirectory, p.AbsolutePath));

        // Loose.sqlproj exists on disk but no solution claims it — the exact thing that surfaces
        // mid-engagement. Setup.wixproj is the reverse: declared by Beta.sln, absent from disk.
        Assert.Contains("Loose/Loose.sqlproj", others);
        Assert.Contains("Setup/Setup.wixproj", others);
    }

    [Fact]
    public void LooseNonCSharpProjectsReachTheNotAssessedList()
    {
        AnalysisResult result = AnalysisHelper.AnalyzeFixture("Estate");

        // The end-to-end consequence: a database project nobody referenced is reported as
        // unassessed rather than being invisible, so coverage is not silently overstated.
        Assert.Contains(result.NotAssessed, p => p.Path == "Loose/Loose.sqlproj");
        Assert.Contains(result.NotAssessed, p => p.Path == "Setup/Setup.wixproj");
    }

    [Fact]
    public void WarnsOnceAboutUnreferencedProjects()
    {
        AnalysisResult result = AnalysisHelper.AnalyzeFixture("Estate");

        ScanWarning warning = Assert.Single(result.Warnings, w => w.Message.Contains("not referenced by any solution"));
        Assert.Contains("Orphan/Orphan.csproj", warning.Message);
    }

    [Fact]
    public void DoesNotWarnAboutOrphansWhenThereAreNoSolutionsToCompareAgainst()
    {
        // Pointed at a bare project directory, everything is unreferenced and saying so is noise.
        AnalysisResult result = AnalysisHelper.AnalyzeFixture("LegacyLibrary");

        Assert.DoesNotContain(result.Warnings, w => w.Message.Contains("not referenced by any solution"));
    }

    [Fact]
    public void DiscoveryIsDeterministic()
    {
        ScanInput first = Estate();
        ScanInput second = Estate();

        Assert.Equal(first.ProjectFiles, second.ProjectFiles);
        Assert.Equal(first.Solutions, second.Solutions);
        Assert.Equal(first.OrphanProjects, second.OrphanProjects);
    }
}
