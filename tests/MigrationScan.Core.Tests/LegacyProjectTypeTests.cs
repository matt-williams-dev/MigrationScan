using MigrationScan.Core.Analysis;
using MigrationScan.Core.Discovery;
using MigrationScan.Core.Models;
using MigrationScan.Core.Rules;

namespace MigrationScan.Core.Tests;

/// <summary>
/// MIG1007 flags legacy project types the solution references (SSRS/SSIS/setup/Silverlight/
/// Web Site); other non-C#/VB projects become "not assessed" warnings. Surfaced by scanning a
/// real solution that included an SSRS reports project and a SQL project.
/// </summary>
public class LegacyProjectTypeTests
{
    // --- unit: SolutionProjectAnalyzer over hand-built entries ---

    private static SolutionProjectEntry Entry(string name, string relativePath, string typeGuid) =>
        new(name, "/repo/" + relativePath, typeGuid);

    private static (IReadOnlyList<Finding> Findings, IReadOnlyList<ScanWarning> Warnings) Analyze(
        params SolutionProjectEntry[] entries) =>
        SolutionProjectAnalyzer.Analyze(entries, "/repo", RuleCatalog.LoadDefault());

    [Fact]
    public void FlagsLegacyTypesByExtension()
    {
        var (findings, warnings) = Analyze(
            Entry("Reports", "Reports/Reports.rptproj", "F14B399A-7131-4C87-9E4B-1186C45EF12D"),
            Entry("Etl", "Etl/Etl.dtproj", "159641D6-6404-4A2A-AE62-294DE0FE8301"),
            Entry("Setup", "Setup/Setup.vdproj", "54435603-DBB4-11D2-8724-00A0C9A8B90C"));

        Assert.Equal(3, findings.Count);
        Assert.All(findings, f => Assert.Equal("MIG1007", f.Rule.Id));
        Assert.Empty(warnings);
        Assert.Contains(findings, f => f.Message.Contains("SSRS"));
    }

    [Fact]
    public void FlagsLegacyTypesByProjectTypeGuid()
    {
        // Silverlight and Web Site have no distinctive extension; detected by type GUID.
        var (findings, _) = Analyze(
            Entry("Sl", "Sl/Sl.csproj", "A1591282-1198-4647-A2B1-27E5FF5F6F3B"),      // Silverlight
            Entry("Web", "Web", "E24C65DC-7377-472B-9ABA-BC803B73C61A"));             // Web Site

        Assert.Equal(2, findings.Count);
        Assert.All(findings, f => Assert.Equal("MIG1007", f.Rule.Id));
    }

    [Fact]
    public void WarnsAboutOtherNonAnalyzableProjects()
    {
        var (findings, warnings) = Analyze(
            Entry("Db", "Db/Db.sqlproj", "00D1A9C2-B5F0-4AF3-8072-F6C62B433612"),
            Entry("Deploy", "Deploy/Deploy.deployproj", "151D2E53-A2C4-4D7D-83FE-D05416EBD58E"));

        Assert.Empty(findings);
        Assert.Equal(2, warnings.Count);
        Assert.All(warnings, w => Assert.Contains("not assessed", w.Message));
    }

    // --- integration: through a real .sln fixture ---

    [Fact]
    public void SolutionScanFlagsLegacyProjectsAndWarnsOnSkipped()
    {
        AnalysisResult result = AnalysisHelper.AnalyzeFixture("MixedProjectTypes", "MixedProjectTypes.sln");

        // App (C#) is scanned and clean; Reports (.rptproj) and Etl (.dtproj) are MIG1007;
        // Db (.sqlproj) is a "not assessed" warning.
        Assert.Single(result.Projects); // only the C# App is an analyzable project
        Assert.Equal(2, result.Findings.Count(f => f.Rule.Id == "MIG1007"));
        Assert.Contains(result.Warnings, w => w.Path!.EndsWith("Db.sqlproj"));
    }
}
