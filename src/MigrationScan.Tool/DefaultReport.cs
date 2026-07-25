using MigrationScan.Core.Models;
using MigrationScan.Reporting;

namespace MigrationScan.Tool;

/// <summary>
/// The no-flags path: a summary on screen and one file to send on.
/// </summary>
/// <remarks>
/// This is the path a customer takes, and they take it without reading any documentation, so it
/// has to be right with zero options supplied. The file covers both portability stances, which is
/// why there is nothing to choose at the command line and nothing to keep track of afterwards.
/// </remarks>
public static class DefaultReport
{
    /// <summary>
    /// The file a default run produces. Fixed, so "which file do I send?" has one answer, and
    /// so a re-run overwrites the previous report rather than accumulating numbered copies
    /// nobody can tell apart.
    /// </summary>
    public const string FileName = "migrationscan-report.json";

    /// <summary>
    /// Where the report goes. A directory receives <see cref="FileName"/>; an explicit file path
    /// is honoured as given; nothing at all means the working directory, which is where somebody
    /// who just double-clicked the executable will look for it.
    /// </summary>
    public static string Destination(string? outputPath) =>
        outputPath is null ? FileName
        : Directory.Exists(outputPath) ? Path.Combine(outputPath, FileName)
        : outputPath;

    /// <summary>Writes the report and returns the closing lines to print.</summary>
    public static IReadOnlyList<string> Write(AnalysisResult result, string? outputPath)
    {
        string destination = Destination(outputPath);
        File.WriteAllText(destination, JsonReportWriter.Write(result));

        return
        [
            $"Report written to {Path.GetFullPath(destination)}",
            "Send this one file on — it covers both staying on Windows and going cross-platform.",
            PrivacyNotice.Summary,
        ];
    }
}
