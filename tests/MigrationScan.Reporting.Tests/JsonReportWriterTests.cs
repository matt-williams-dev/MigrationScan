using System.Text.Json;
using MigrationScan.Core.Models;
using MigrationScan.Reporting;

namespace MigrationScan.Reporting.Tests;

public class JsonReportWriterTests
{
    private static AnalysisResult SampleResult()
    {
        RuleMetadata rule = new(
            Id: "MIG1001",
            Title: "Non-SDK-style project file",
            Category: "Project and build",
            Severity: Severity.Medium,
            Effort: EffortBand.Small,
            Tier: ConfidenceTier.Certain,
            Remediation: "Convert to the SDK style.",
            DocsUrl: "https://example.test/MIG1001");

        Finding finding = new(
            Rule: rule,
            Message: "Project 'Legacy' uses the legacy non-SDK project format.",
            ProjectPath: "Legacy/Legacy.csproj",
            FilePath: "Legacy/Legacy.csproj",
            Line: 2);

        DiscoveredProject project = new(
            Path: "Legacy/Legacy.csproj",
            Name: "Legacy",
            IsSdkStyle: false,
            TargetFramework: "v4.7.2",
            References: ["System.Web"],
            RootElementLine: 2);

        return new AnalysisResult("net10.0", [project], [finding], [])
        {
            NotAssessed =
            [
                new NotAssessedProject("Shop.Database", "Shop.Database/Shop.Database.sqlproj",
                    "SQL Server database project", "Not a C#/VB project; must be scoped separately."),
            ],
        };
    }

    [Fact]
    public void ProducesValidJsonWithExpectedSchema()
    {
        string json = JsonReportWriter.Write(SampleResult());

        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;

        Assert.Equal("1.2", root.GetProperty("schemaVersion").GetString());
        Assert.Equal("net10.0", root.GetProperty("target").GetString());

        JsonElement summary = root.GetProperty("summary");
        Assert.Equal(1, summary.GetProperty("projectsScanned").GetInt32());
        Assert.Equal(1, summary.GetProperty("totalFindings").GetInt32());
        Assert.Equal(1, summary.GetProperty("findingsBySeverity").GetProperty("medium").GetInt32());
        Assert.Equal(0, summary.GetProperty("findingsBySeverity").GetProperty("blocker").GetInt32());

        // Effort rollup (schema 1.1): one Small finding -> 0.5–2 days, nothing needing a decision.
        JsonElement effort = summary.GetProperty("effort");
        Assert.Equal(0.5, effort.GetProperty("minDays").GetDouble());
        Assert.Equal(2, effort.GetProperty("maxDays").GetDouble());
        Assert.Equal(0, effort.GetProperty("needsDecision").GetInt32());

        JsonElement project = Assert.Single(root.GetProperty("projects").EnumerateArray().ToList());
        Assert.Equal("Legacy/Legacy.csproj", project.GetProperty("path").GetString());
        Assert.Equal(1, project.GetProperty("findingCount").GetInt32());
        Assert.Equal(2, project.GetProperty("effort").GetProperty("maxDays").GetDouble());

        JsonElement finding = Assert.Single(root.GetProperty("findings").EnumerateArray().ToList());
        Assert.Equal("MIG1001", finding.GetProperty("ruleId").GetString());
        Assert.Equal("medium", finding.GetProperty("severity").GetString());
        Assert.Equal("certain", finding.GetProperty("tier").GetString());
        Assert.Equal("small", finding.GetProperty("effort").GetString());
        Assert.Equal("Legacy/Legacy.csproj", finding.GetProperty("project").GetString());
        Assert.Equal(2, finding.GetProperty("line").GetInt32());

        // Not-assessed projects (schema 1.2): structured entry + a summary count.
        Assert.Equal(1, summary.GetProperty("projectsNotAssessed").GetInt32());
        JsonElement notAssessed = Assert.Single(root.GetProperty("notAssessed").EnumerateArray().ToList());
        Assert.Equal("Shop.Database", notAssessed.GetProperty("name").GetString());
        Assert.Equal("SQL Server database project", notAssessed.GetProperty("projectType").GetString());
        Assert.EndsWith(".sqlproj", notAssessed.GetProperty("path").GetString());

        // The warnings array is always present (empty here) for schema stability.
        Assert.Equal(JsonValueKind.Array, root.GetProperty("warnings").ValueKind);
        Assert.Empty(root.GetProperty("warnings").EnumerateArray());
    }

    [Fact]
    public void OutputIsDeterministic()
    {
        string first = JsonReportWriter.Write(SampleResult());
        string second = JsonReportWriter.Write(SampleResult());

        Assert.Equal(first, second);
    }
}
