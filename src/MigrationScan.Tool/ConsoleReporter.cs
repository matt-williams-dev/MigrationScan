using System.Text;
using MigrationScan.Core.Models;

namespace MigrationScan.Tool;

/// <summary>
/// Renders the console summary (spec §8): counts by severity, then findings grouped by
/// rule and ordered most-severe-first (so a rule that fires many times doesn't bury the
/// structural findings), and the mandatory reminder that effort figures are not a quote.
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

        if (result.Warnings.Count > 0)
        {
            output.AppendLine();
            output.AppendLine($"Warnings ({result.Warnings.Count}):");
            foreach (ScanWarning warning in result.Warnings)
            {
                output.AppendLine($"  ! {warning.Message}");
            }
        }

        if (result.Findings.Count == 0)
        {
            output.AppendLine();
            output.AppendLine("No findings. Nothing here blocks a move off .NET Framework.");
            return output.ToString();
        }

        // Group repeated findings of one rule so a rule that fires many times (e.g. a config
        // API used across a codebase) doesn't bury the structural findings under duplicated
        // remediation text. Most-severe rules first.
        var groups = result.Findings
            .GroupBy(f => f.Rule.Id)
            .Select(g => (Rule: g.First().Rule, Items: g.ToList()))
            .OrderBy(g => g.Rule.Severity)
            .ThenBy(g => g.Rule.Id, StringComparer.Ordinal);

        foreach ((RuleMetadata rule, List<Finding> group) in groups)
        {
            // Cluster locations by file (then line) so occurrences in the same file sit together.
            List<Finding> items = group
                .OrderBy(f => f.FilePath ?? f.ProjectPath, StringComparer.Ordinal)
                .ThenBy(f => f.Line ?? 0)
                .ToList();

            output.AppendLine();
            string occurrences = items.Count == 1 ? string.Empty : $"  ({items.Count} occurrences)";
            output.AppendLine(
                $"{rule.Id}  {Lower(rule.Severity)} · {Lower(rule.Tier)} · effort {Lower(rule.Effort)}{occurrences}");
            output.AppendLine($"  {rule.Title}");

            List<string> distinctMessages = items.Select(i => i.Message).Distinct().ToList();
            if (distinctMessages.Count == 1)
            {
                // Every occurrence says the same thing: show it once, then just the locations.
                output.AppendLine($"  {distinctMessages[0]}");
                foreach (Finding item in items)
                {
                    output.AppendLine($"    {Location(item)}");
                }
            }
            else
            {
                // Messages differ per site (e.g. a package or assembly name): show each.
                foreach (Finding item in items)
                {
                    output.AppendLine($"    {Location(item)} — {item.Message}");
                }
            }

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
