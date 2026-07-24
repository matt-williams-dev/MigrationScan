using MigrationScan.Core.Models;

namespace MigrationScan.Reporting.Tests;

public class DeterminismTests
{
    [Fact]
    public void MarkdownUsesLfNewlinesOnly()
    {
        string markdown = MarkdownReportWriter.Write(ReportSample.Build());
        Assert.DoesNotContain('\r', markdown);
    }

    [Fact]
    public void JsonUsesLfNewlinesOnly()
    {
        string json = JsonReportWriter.Write(ReportSample.Build());
        Assert.DoesNotContain('\r', json);
    }

    [Fact]
    public void ReportsAreStableAcrossRuns()
    {
        AnalysisResult result = ReportSample.Build();
        Assert.Equal(MarkdownReportWriter.Write(result), MarkdownReportWriter.Write(result));
        Assert.Equal(JsonReportWriter.Write(result), JsonReportWriter.Write(result));
    }
}
