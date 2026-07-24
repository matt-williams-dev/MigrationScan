using MigrationScan.Core.Models;
using MigrationScan.Tool;

namespace MigrationScan.Tool.Tests;

public class ConsoleReporterTests
{
    [Fact]
    public void GroupsRepeatedRuleWithOccurrenceCountAndSingleRemediation()
    {
        RuleMetadata rule = Rule("MIG5001", Severity.Low, "Reads config.", "Use Options pattern.");
        AnalysisResult result = Result(
            Finding(rule, "Reads via ConfigurationManager.", "P/A.cs", 10),
            Finding(rule, "Reads via ConfigurationManager.", "P/A.cs", 20),
            Finding(rule, "Reads via ConfigurationManager.", "P/B.cs", 5));

        string output = ConsoleReporter.Render(result);

        Assert.Contains("MIG5001", output);
        Assert.Contains("(3 occurrences)", output);
        Assert.Equal(1, CountOccurrences(output, "Use Options pattern.")); // remediation once
        Assert.Equal(1, CountOccurrences(output, "Reads via ConfigurationManager.")); // shared message once
        Assert.Contains("P/A.cs:10", output);
        Assert.Contains("P/B.cs:5", output);
    }

    [Fact]
    public void OrdersGroupsMostSevereFirst()
    {
        RuleMetadata low = Rule("MIG5001", Severity.Low, "low", "r");
        RuleMetadata blocker = Rule("MIG3001", Severity.Blocker, "blocker", "r");
        AnalysisResult result = Result(
            Finding(low, "m", "P/A.cs", 1),
            Finding(blocker, "m", "P/B.cs", 1));

        string output = ConsoleReporter.Render(result);

        Assert.True(output.IndexOf("MIG3001", StringComparison.Ordinal) < output.IndexOf("MIG5001", StringComparison.Ordinal),
            "Blocker-severity rule should be rendered before the low-severity rule.");
    }

    [Fact]
    public void ShowsPerSiteMessageWhenMessagesDiffer()
    {
        RuleMetadata rule = Rule("MIG2001", Severity.High, "Incompatible package.", "Replace it.");
        AnalysisResult result = Result(
            Finding(rule, "Package 'Foo' is incompatible.", "P/packages.config", 2),
            Finding(rule, "Package 'Bar' is incompatible.", "P/packages.config", 3));

        string output = ConsoleReporter.Render(result);

        Assert.Contains("Package 'Foo' is incompatible.", output);
        Assert.Contains("Package 'Bar' is incompatible.", output);
    }

    [Fact]
    public void SingleOccurrenceHasNoOccurrenceCount()
    {
        AnalysisResult result = Result(Finding(Rule("MIG1001", Severity.Medium, "m", "r"), "m", "P/P.csproj", 2));

        string output = ConsoleReporter.Render(result);

        Assert.DoesNotContain("occurrence", output);
    }

    [Fact]
    public void NoFindingsRendersCleanMessage()
    {
        AnalysisResult result = new("net10.0", [], [], []);

        string output = ConsoleReporter.Render(result);

        Assert.Contains("No findings", output);
    }

    [Fact]
    public void ShowsWarnings()
    {
        AnalysisResult result = new("net10.0", [], [], [new ScanWarning("Skipped 'X': not found.", "X")]);

        string output = ConsoleReporter.Render(result);

        Assert.Contains("Warnings (1)", output);
        Assert.Contains("Skipped 'X': not found.", output);
    }

    private static AnalysisResult Result(params Finding[] findings) =>
        new("net10.0", [], findings, []);

    private static RuleMetadata Rule(string id, Severity severity, string title, string remediation) =>
        new(id, title, "Category", severity, EffortBand.Small, ConfidenceTier.Probable, remediation, "https://example.test");

    private static Finding Finding(RuleMetadata rule, string message, string file, int line) =>
        new(rule, message, "P/P.csproj", file, line);

    private static int CountOccurrences(string haystack, string needle)
    {
        int count = 0;
        int index = 0;
        while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }

        return count;
    }
}
