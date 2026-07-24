using VerifyTests;

namespace MigrationScan.Reporting.Tests;

/// <summary>
/// Golden-file tests (spec §12): the full report output is snapshotted so any change to a
/// report's wording or structure shows up as a visible diff in review.
/// </summary>
public class GoldenReportTests
{
    [Fact]
    public Task MarkdownReport()
    {
        string markdown = MarkdownReportWriter.Write(ReportSample.Build());
        return Verify(new Target("md", markdown));
    }

    [Fact]
    public Task JsonReport()
    {
        string json = JsonReportWriter.Write(ReportSample.Build());
        return Verify(new Target("json", json));
    }

    [Fact]
    public Task SarifReport()
    {
        // SARIF is JSON; snapshot with the json extension (Verify treats .sarif as binary).
        string sarif = SarifReportWriter.Write(ReportSample.Build());
        return Verify(new Target("json", sarif));
    }

    // Windows-target variant: exercises the "satisfied by target" sections and the
    // active-only counts/effort across all three formats.

    [Fact]
    public Task MarkdownReportWindowsTarget()
    {
        string markdown = MarkdownReportWriter.Write(ReportSample.BuildWindowsTarget());
        return Verify(new Target("md", markdown));
    }

    [Fact]
    public Task JsonReportWindowsTarget()
    {
        string json = JsonReportWriter.Write(ReportSample.BuildWindowsTarget());
        return Verify(new Target("json", json));
    }

    [Fact]
    public Task SarifReportWindowsTarget()
    {
        string sarif = SarifReportWriter.Write(ReportSample.BuildWindowsTarget());
        return Verify(new Target("json", sarif));
    }
}
