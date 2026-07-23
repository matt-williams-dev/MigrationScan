using System.Text;
using MigrationScan.Core.Models;

namespace MigrationScan.Tool;

/// <summary>
/// Renders the console summary (spec §8): counts by severity, the findings, and the
/// mandatory reminder that effort figures are planning aids, not a quote.
/// </summary>
internal static class ConsoleReporter
{
    private const string Disclaimer =
        "These figures are heuristic planning aids derived from static analysis and are not a quote.";

    public static string Render(AnalysisResult result)
    {
        StringBuilder output = new();

        output.AppendLine($"MigrationScan — target {result.Target}");
        output.AppendLine();

        int projectCount = result.Projects.Count;
        output.AppendLine($"Scanned {projectCount} {Plural(projectCount, "project", "projects")}.");
        output.AppendLine();

        IReadOnlyDictionary<Severity, int> counts = result.CountsBySeverity();
        output.AppendLine(
            $"Findings by severity:  blocker {counts[Severity.Blocker]} · high {counts[Severity.High]} · " +
            $"medium {counts[Severity.Medium]} · low {counts[Severity.Low]}   ({result.Findings.Count} total)");

        if (result.Findings.Count == 0)
        {
            output.AppendLine();
            output.AppendLine("No findings. Nothing here blocks a move off .NET Framework.");
            return output.ToString();
        }

        foreach (Finding finding in result.Findings)
        {
            RuleMetadata rule = finding.Rule;
            output.AppendLine();
            output.AppendLine(
                $"{rule.Id}  {Lower(rule.Severity)} · {Lower(rule.Tier)} · effort {Lower(rule.Effort)}");
            output.AppendLine($"  {rule.Title}");
            output.AppendLine($"  {finding.Message}");
            output.AppendLine($"  {Location(finding)}");
            output.AppendLine($"  → {rule.Remediation}");
        }

        output.AppendLine();
        output.AppendLine(Disclaimer);
        return output.ToString();
    }

    private static string Location(Finding finding)
    {
        string file = finding.FilePath ?? finding.ProjectPath;
        return finding.Line is { } line ? $"{file}:{line}" : file;
    }

    private static string Plural(int count, string singular, string plural) => count == 1 ? singular : plural;

    private static string Lower<TEnum>(TEnum value) where TEnum : struct, Enum =>
        value.ToString().ToLowerInvariant();
}
