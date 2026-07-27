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

    [Fact]
    public void SummaryRowsAlignOnOneColumn()
    {
        AnalysisResult result = Result(
            Finding(Rule("MIG1001", Severity.Medium, "m", "r"), "m", "P/P.csproj", 2));

        List<string> block = SummaryBlock(ConsoleReporter.Render(result));

        // Every row's value starts at the same column, which is the entire point of the block:
        // a reader takes it in vertically instead of parsing prose.
        Assert.True(block.Count > 1, "Expected several summary rows to compare.");
        Assert.Single(block.Select(ValueColumn).Distinct());
    }

    /// <summary>The index where a row's value begins, after the label and its padding.</summary>
    private static int ValueColumn(string line)
    {
        int gap = line.IndexOf("  ", StringComparison.Ordinal);
        Assert.True(gap > 0, $"Row has no label/value gap: '{line}'");
        return line.Length - line[gap..].TrimStart().Length;
    }

    [Fact]
    public void PricesTheOtherStanceWithoutRescanning()
    {
        RuleMetadata registry = Windows("MIG4002", "Registry access");
        AnalysisResult result = Result(
            Finding(Rule("MIG1001", Severity.Medium, "m", "r"), "m", "P/P.csproj", 2),
            Finding(registry, "Reads the registry.", "P/A.cs", 5));

        string output = ConsoleReporter.Render(result);

        // Scanned cross-platform, so the reader is told what staying on Windows would cost.
        Assert.Contains("Staying on Windows", output);
        Assert.Contains("1 fewer", output);
        Assert.Contains("all in one project", output);
    }

    [Fact]
    public void InvertsTheStanceRowWhenTheTargetIsWindows()
    {
        RuleMetadata registry = Windows("MIG4002", "Registry access");
        AnalysisResult result = new("net10.0-windows", [],
            [Finding(Rule("MIG1001", Severity.Medium, "m", "r"), "m", "P/P.csproj", 2),
             Finding(registry, "Reads the registry.", "P/A.cs", 5)], []);

        string output = ConsoleReporter.Render(result.ForTarget("net10.0-windows"));

        // Asked about Windows, so the unasked question is the cross-platform one.
        Assert.Contains("Going cross-platform", output);
        Assert.Contains("1 more", output);
        Assert.DoesNotContain("Staying on Windows", output);
    }

    [Fact]
    public void SaysSoRatherThanRepeatingItselfWhenBothStancesMatch()
    {
        // No Windows lock-in anywhere, so the two stances price identically. A row of numbers
        // duplicating the row above it would read as a bug.
        AnalysisResult result = Result(
            Finding(Rule("MIG1001", Severity.Medium, "m", "r"), "m", "P/P.csproj", 2));

        string output = ConsoleReporter.Render(result);

        Assert.Contains("no change · nothing here is Windows-only", output);
        Assert.DoesNotContain("fewer", output);
    }

    [Fact]
    public void SummarisesManySkippedProjectsByKindRatherThanListingThem()
    {
        AnalysisResult result = Result(Finding(Rule("MIG1001", Severity.Medium, "m", "r"), "m", "P/P.csproj", 2))
            with
        {
            NotAssessed =
            [
                new NotAssessedProject("A", "a/A.dcproj", "DCPROJ project", "unsupported"),
                new NotAssessedProject("B", "b/B.dcproj", "DCPROJ project", "unsupported"),
                new NotAssessedProject("C", "c/C.sfproj", "SFPROJ project", "unsupported"),
            ],
        };

        string output = ConsoleReporter.Render(result);

        Assert.Contains("2 .dcproj, 1 .sfproj", output);
    }

    [Fact]
    public void NamesASingleSkippedProject()
    {
        AnalysisResult result = Result(Finding(Rule("MIG1001", Severity.Medium, "m", "r"), "m", "P/P.csproj", 2))
            with
        {
            NotAssessed = [new NotAssessedProject("Ledger.Database", "db/Ledger.Database.sqlproj", "SQL", "unsupported")],
        };

        string output = ConsoleReporter.Render(result);

        Assert.Contains("Ledger.Database (.sqlproj)", output);
    }

    [Fact]
    public void OneVeryLongProjectNameCannotStretchTheBlock()
    {
        string huge = new('x', 120);
        AnalysisResult result = Result(Finding(Rule("MIG1001", Severity.Medium, "m", "r"), "m", "P/P.csproj", 2))
            with
        {
            NotAssessed = [new NotAssessedProject(huge, $"db/{huge}.sqlproj", "SQL", "unsupported")],
        };

        List<string> block = SummaryBlock(ConsoleReporter.Render(result));

        // Terminals are 80 columns often enough that one pathological name must not decide the
        // layout for everyone. The full name still appears in the detailed list below the block.
        Assert.All(block, line => Assert.True(line.Length <= 80, $"Row is {line.Length} columns: '{line}'"));
        Assert.Contains(block, l => l.Contains('…', StringComparison.Ordinal));
    }

    // The lines between the header and the first blank line after it.
    private static List<string> SummaryBlock(string output) =>
        output.Split(Environment.NewLine)
            .SkipWhile(l => !l.StartsWith("Projects scanned", StringComparison.Ordinal))
            .TakeWhile(l => l.Length > 0)
            .ToList();

    private static AnalysisResult Result(params Finding[] findings) =>
        new("net10.0", [], findings, []);

    private static RuleMetadata Windows(string id, string title) =>
        new(id, title, "Runtime failures", Severity.High, EffortBand.Small, ConfidenceTier.Probable,
            "r", "https://example.test") { Platform = RulePlatform.Windows };

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
