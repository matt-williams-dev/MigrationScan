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

        return new AnalysisResult("net10.0", [project], [finding]);
    }

    [Fact]
    public void ProducesValidJsonWithExpectedSchema()
    {
        string json = JsonReportWriter.Write(SampleResult());

        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;

        Assert.Equal("1.0", root.GetProperty("schemaVersion").GetString());
        Assert.Equal("net10.0", root.GetProperty("target").GetString());

        JsonElement summary = root.GetProperty("summary");
        Assert.Equal(1, summary.GetProperty("projectsScanned").GetInt32());
        Assert.Equal(1, summary.GetProperty("totalFindings").GetInt32());
        Assert.Equal(1, summary.GetProperty("findingsBySeverity").GetProperty("medium").GetInt32());
        Assert.Equal(0, summary.GetProperty("findingsBySeverity").GetProperty("blocker").GetInt32());

        JsonElement finding = Assert.Single(root.GetProperty("findings").EnumerateArray().ToList());
        Assert.Equal("MIG1001", finding.GetProperty("ruleId").GetString());
        Assert.Equal("medium", finding.GetProperty("severity").GetString());
        Assert.Equal("certain", finding.GetProperty("tier").GetString());
        Assert.Equal("small", finding.GetProperty("effort").GetString());
        Assert.Equal("Legacy/Legacy.csproj", finding.GetProperty("project").GetString());
        Assert.Equal(2, finding.GetProperty("line").GetInt32());
    }

    [Fact]
    public void OutputIsDeterministic()
    {
        string first = JsonReportWriter.Write(SampleResult());
        string second = JsonReportWriter.Write(SampleResult());

        Assert.Equal(first, second);
    }
}
