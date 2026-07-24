using System.Text.Json;
using MigrationScan.Core.Models;

namespace MigrationScan.Reporting.Tests;

public class SarifReportWriterTests
{
    private static JsonElement Root(AnalysisResult result) =>
        JsonDocument.Parse(SarifReportWriter.Write(result)).RootElement;

    [Fact]
    public void EmitsSarif210WithToolAndResults()
    {
        JsonElement root = Root(ReportSample.Build());

        Assert.Equal("2.1.0", root.GetProperty("version").GetString());
        JsonElement run = root.GetProperty("runs").EnumerateArray().Single();
        Assert.Equal("MigrationScan", run.GetProperty("tool").GetProperty("driver").GetProperty("name").GetString());
        Assert.NotEmpty(run.GetProperty("results").EnumerateArray().ToList());
    }

    [Fact]
    public void RulesCarryHelpUriAndResultsIndexIntoThem()
    {
        JsonElement run = Root(ReportSample.Build()).GetProperty("runs").EnumerateArray().Single();
        List<JsonElement> rules = run.GetProperty("tool").GetProperty("driver").GetProperty("rules").EnumerateArray().ToList();
        List<JsonElement> results = run.GetProperty("results").EnumerateArray().ToList();

        Assert.All(rules, r => Assert.StartsWith("https://", r.GetProperty("helpUri").GetString()!));

        foreach (JsonElement result in results)
        {
            int index = result.GetProperty("ruleIndex").GetInt32();
            Assert.Equal(result.GetProperty("ruleId").GetString(), rules[index].GetProperty("id").GetString());
        }
    }

    [Fact]
    public void MapsSeverityToSarifLevel()
    {
        JsonElement run = Root(ReportSample.Build()).GetProperty("runs").EnumerateArray().Single();
        List<JsonElement> results = run.GetProperty("results").EnumerateArray().ToList();

        string LevelOf(string ruleId) => results
            .First(r => r.GetProperty("ruleId").GetString() == ruleId)
            .GetProperty("level").GetString()!;

        Assert.Equal("error", LevelOf("MIG3001"));   // Blocker -> error
        Assert.Equal("warning", LevelOf("MIG7001")); // Medium -> warning
        Assert.Equal("note", LevelOf("MIG5001"));    // Low -> note
    }

    [Fact]
    public void UsesLfNewlinesOnly() =>
        Assert.DoesNotContain('\r', SarifReportWriter.Write(ReportSample.Build()));
}
