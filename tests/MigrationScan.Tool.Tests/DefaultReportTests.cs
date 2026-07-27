using System.Text.Json;
using MigrationScan.Core.Analysis;
using MigrationScan.Core.Models;
using MigrationScan.Tool;

namespace MigrationScan.Tool.Tests;

/// <summary>
/// The zero-flag path. A customer reaches this without reading anything, so what it writes and
/// where it writes it are part of the contract, not incidental behaviour.
/// </summary>
public class DefaultReportTests
{
    [Fact]
    public void WritesToTheWorkingDirectoryWhenNoOutputIsGiven()
    {
        Assert.Equal(DefaultReport.FileName, DefaultReport.Destination(null));
    }

    [Fact]
    public void ADirectoryReceivesTheStandardFileName()
    {
        string directory = Directory.CreateTempSubdirectory().FullName;
        try
        {
            Assert.Equal(Path.Combine(directory, DefaultReport.FileName), DefaultReport.Destination(directory));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void AnExplicitFilePathIsHonouredAsGiven()
    {
        Assert.Equal("reports/acme.json", DefaultReport.Destination("reports/acme.json"));
    }

    [Fact]
    public void WritesACombinedReportAndTellsTheUserWhereItWent()
    {
        string directory = Directory.CreateTempSubdirectory().FullName;
        try
        {
            AnalysisResult result = SolutionAnalyzer.CreateDefault()
                .Analyze(FixturePath("NativeInterop"), "net10.0");

            IReadOnlyList<string> lines = DefaultReport.Write(result, directory);

            string path = Path.Combine(directory, DefaultReport.FileName);
            Assert.True(File.Exists(path));
            Assert.Contains(lines, l => l.Contains(path));

            // The point of the whole change: one file answers both portability questions, so
            // nobody has to run the tool twice or keep track of which file was which.
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
            List<JsonElement> targets = document.RootElement.GetProperty("targets").EnumerateArray().ToList();
            Assert.Equal(["net10.0", "net10.0-windows"], targets.Select(t => t.GetProperty("target").GetString()));

            // ...and this fixture is Windows interop, so the two stances must genuinely differ —
            // otherwise the test would pass just as well against a report that ignored the stance.
            Assert.True(
                targets[0].GetProperty("summary").GetProperty("totalFindings").GetInt32() >
                targets[1].GetProperty("summary").GetProperty("totalFindings").GetInt32());
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void SaysWhatIsSafeToShare()
    {
        string directory = Directory.CreateTempSubdirectory().FullName;
        try
        {
            AnalysisResult result = SolutionAnalyzer.CreateDefault()
                .Analyze(FixturePath("NativeInterop"), "net10.0");

            IReadOnlyList<string> lines = DefaultReport.Write(result, directory);

            // A customer has to clear this file with their security people before sending it, so
            // the claim on screen has to be the one the file backs up: source locations go, and
            // the projects and dependencies stay.
            Assert.Contains(lines, l => l.Contains("no source code · source file paths as opaque ids"));

            // Said once. Two lines making the same promise trains people to read neither, and the
            // console block above already prices both stances.
            Assert.Single(lines);

            // ...and the file has to actually back it up.
            string report = File.ReadAllText(Path.Combine(directory, DefaultReport.FileName));
            Assert.DoesNotContain("ScannerInterop.cs", report, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string FixturePath(string name)
    {
        for (DirectoryInfo? d = new(AppContext.BaseDirectory); d is not null; d = d.Parent)
        {
            string candidate = Path.Combine(d.FullName, "tests", "fixtures", name);
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new DirectoryNotFoundException($"Could not locate tests/fixtures/{name}.");
    }
}
