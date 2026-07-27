using System.Globalization;
using System.Text;
using MigrationScan.Core.Effort;
using MigrationScan.Core.Models;
using MigrationScan.Reporting;

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

        output.AppendLine($"MigrationScan · target {result.Target}");
        output.AppendLine();

        int activeCount = result.ActiveFindings.Count();
        int satisfiedCount = result.SatisfiedFindings.Count();
        WriteSummary(output, result);

        if (result.NotAssessed.Count > 0)
        {
            output.AppendLine();
            output.AppendLine($"Not assessed, scope separately ({result.NotAssessed.Count}):");
            foreach (NotAssessedProject project in result.NotAssessed)
            {
                output.AppendLine($"  • {project.Name} ({project.ProjectType}) · {project.Path}");
            }
        }

        if (result.Warnings.Count > 0)
        {
            output.AppendLine();
            output.AppendLine($"Warnings ({result.Warnings.Count}):");
            foreach (ScanWarning warning in result.Warnings)
            {
                output.AppendLine($"  ! {warning.Message}");
            }
        }

        WriteReferences(output, result);

        if (activeCount == 0 && satisfiedCount == 0 && result.NotAssessed.Count == 0)
        {
            output.AppendLine();
            output.AppendLine("No findings. Nothing here blocks a move off .NET Framework.");
            return output.ToString();
        }

        // Group repeated findings of one rule so a rule that fires many times (e.g. a config
        // API used across a codebase) doesn't bury the structural findings under duplicated
        // remediation text. Most-severe rules first. Findings satisfied by the target are
        // handled in their own section below, so they don't inflate the main list.
        var groups = result.ActiveFindings
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
                    output.AppendLine($"    {Location(item)} · {item.Message}");
                }
            }

            output.AppendLine($"  → {rule.Remediation}");
        }

        WriteSatisfiedByTarget(output, result);

        output.AppendLine();
        output.AppendLine(Disclaimer);
        return output.ToString();
    }

    /// <summary>
    /// The summary block: one aligned label-value line per fact, so the whole shape of the estate
    /// reads in a single pass rather than as prose to parse.
    /// </summary>
    /// <remarks>
    /// The last row is the one a first-time reader most needs and would otherwise never see. The
    /// console shows one stance, so without it the tool's whole argument — that one scan prices
    /// both futures — stays buried in a JSON file nobody has opened yet. It is derived from the
    /// same findings, not a second scan.
    /// </remarks>
    private static void WriteSummary(StringBuilder output, AnalysisResult result)
    {
        List<Row> rows = [];

        rows.Add(new Row("Projects scanned", Count(result.Projects.Count), string.Empty));

        IReadOnlyDictionary<Severity, int> counts = result.CountsBySeverity();
        rows.Add(new Row(
            "Findings",
            Count(result.ActiveFindings.Count()),
            $"blocker {counts[Severity.Blocker]} · high {counts[Severity.High]} · " +
            $"medium {counts[Severity.Medium]} · low {counts[Severity.Low]}"));

        int satisfied = result.SatisfiedFindings.Count();
        if (satisfied > 0)
        {
            rows.Add(new Row("Satisfied by target", Count(satisfied), $"Windows lock-in, supported on {result.Target}"));
        }

        EffortEstimate effort = EffortModel.ForSolution(result);
        rows.Add(new Row("Estimated effort", EffortFormat.DaysWithUnit(effort), string.Empty));

        if (effort.BlockerCount > 0)
        {
            rows.Add(new Row("Needs decision", Count(effort.BlockerCount), "architectural, left unpriced"));
        }

        if (result.NotAssessed.Count > 0)
        {
            rows.Add(new Row("Not assessed", Count(result.NotAssessed.Count), NotAssessedDetail(result.NotAssessed)));
        }

        int thirdParty = result.DistinctThirdPartyCount();
        if (thirdParty > 0)
        {
            rows.Add(new Row("Third-party", Count(thirdParty), "distinct references, inventory only"));
        }

        rows.Add(OtherStance(result));

        // Two columns, each sized to what it actually holds. The value column is measured only
        // across rows that carry a detail, so the long effort string does not push every detail
        // off to the right of a narrow terminal.
        int labelWidth = rows.Max(r => r.Label.Length);
        int valueWidth = rows.Where(r => r.Detail.Length > 0).Select(r => r.Value.Length).DefaultIfEmpty(0).Max();

        foreach (Row row in rows)
        {
            string line = row.Detail.Length == 0
                ? $"{row.Label.PadRight(labelWidth)}  {row.Value}"
                : $"{row.Label.PadRight(labelWidth)}  {row.Value.PadRight(valueWidth)}   {row.Detail}";
            output.AppendLine(line.TrimEnd());
        }
    }

    /// <summary>
    /// The stance the reader did not ask for, priced from the same findings.
    /// </summary>
    /// <remarks>
    /// An estate with no Windows lock-in prices identically either way. Printing a row of numbers
    /// matching the row above it would read as a bug, so that case says the thing worth knowing
    /// instead: leaving Windows costs nothing here.
    /// </remarks>
    private static Row OtherStance(AnalysisResult result)
    {
        (string crossPlatform, string windows) = TargetPlatform.Stances(result.Target);
        bool onWindows = TargetPlatform.IsWindows(result.Target);
        string otherTarget = onWindows ? crossPlatform : windows;

        AnalysisResult other = result.ForTarget(otherTarget);
        int otherCount = other.ActiveFindings.Count();
        int difference = Math.Abs(otherCount - result.ActiveFindings.Count());
        string label = onWindows ? "Going cross-platform" : "Staying on Windows";

        if (difference == 0)
        {
            return new Row(label, "no change · nothing here is Windows-only", string.Empty);
        }

        // The findings that differ are exactly the Windows lock-in ones. Naming where they sit
        // turns a number into a scope: six findings in one project is an afternoon, six spread
        // across six projects is a planning problem.
        List<string> projects = result.Findings
            .Where(f => f.Rule.Platform == RulePlatform.Windows)
            .Select(f => f.ProjectPath)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        string where = projects.Count == 1 ? "all in one project" : $"in {projects.Count} projects";
        string direction = onWindows ? "more" : "fewer";
        // Short unit here: "engineer-days" is spelled out on the Estimated effort row above,
        // and those columns are what keep this row inside an 80-column terminal.
        string effort = EffortFormat.DaysShort(EffortModel.ForSolution(other));

        return new Row(label, $"{otherCount} findings · {effort} · {difference} {direction}, {where}", string.Empty);
    }

    /// <summary>
    /// What was skipped, in one phrase. A single project is worth naming; several are worth
    /// counting by kind, because "4 .dcproj, 3 .sfproj" says what sort of work it is and a list
    /// of seven names does not. Every one is listed in full below either way.
    /// </summary>
    private static string NotAssessedDetail(IReadOnlyList<NotAssessedProject> projects)
    {
        if (projects.Count == 1)
        {
            NotAssessedProject only = projects[0];
            return $"{Truncate(only.Name, 32)} ({Extension(only.Path)})";
        }

        return string.Join(", ", projects
            .GroupBy(p => Extension(p.Path), StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => $"{g.Count()} {g.Key}"));
    }

    private static string Extension(string path)
    {
        string extension = Path.GetExtension(path);
        return extension.Length == 0 ? "no extension" : extension;
    }

    // Long enough for a real project name, short enough that one pathological name cannot push
    // the block off the side of a terminal.
    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..(max - 1)] + "…";

    private static string Count(int value) => value.ToString(CultureInfo.InvariantCulture);

    private readonly record struct Row(string Label, string Value, string Detail);

    // The third-party dependency catalog. Inventory, not findings: the list to research for
    // modern .NET support. Framework assemblies and this solution's own project references are
    // excluded (noted, not silently dropped); the full inventory is in the JSON and Markdown.
    private static void WriteReferences(StringBuilder output, AnalysisResult result)
    {
        var groups = result.ThirdPartyReferences
            .GroupBy(r => (r.Kind, Key: r.Name.ToUpperInvariant()))
            .Select(g => new
            {
                g.First().Kind,
                g.First().Name,
                Versions = g.Select(r => r.Version).OfType<string>().Distinct(StringComparer.Ordinal)
                    .OrderBy(v => v, StringComparer.Ordinal).ToList(),
                Projects = g.Select(r => r.ProjectPath).Distinct(StringComparer.Ordinal).Count(),
            })
            .OrderBy(g => g.Kind)
            .ThenBy(g => g.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (groups.Count == 0)
        {
            return;
        }

        output.AppendLine();
        output.AppendLine($"Third-party references ({groups.Count} distinct), inventory only, not counted above:");

        foreach (var group in groups)
        {
            string version = group.Versions.Count == 0 ? string.Empty : $" {string.Join(", ", group.Versions)}";
            string projects = group.Projects == 1 ? string.Empty : $"  [{group.Projects} projects]";
            output.AppendLine($"  • {KindLabel(group.Kind)}  {group.Name}{version}{projects}");
        }

        int frameworkCount = result.References.Count(r => r.IsFrameworkAssembly);
        int projectCount = result.References.Count(r => r.Kind == ReferenceKind.Project);
        if (frameworkCount > 0 || projectCount > 0)
        {
            output.AppendLine(
                $"  (Also read, not listed: {frameworkCount} framework, {projectCount} solution-internal.)");
        }
    }

    private static string KindLabel(ReferenceKind kind) => kind switch
    {
        ReferenceKind.Package => "nuget ",
        ReferenceKind.Assembly => "gac   ",
        ReferenceKind.VendoredAssembly => "dll   ",
        ReferenceKind.Com => "com   ",
        ReferenceKind.Project => "proj  ",
        ReferenceKind.WebService => "svc   ",
        _ => "?     ",
    };

    // Windows lock-in findings that this (Windows) target satisfies: shown so the scope is
    // complete, but clearly marked as not migration cost for this target.
    private static void WriteSatisfiedByTarget(StringBuilder output, AnalysisResult result)
    {
        List<Finding> satisfied = result.SatisfiedFindings.ToList();
        if (satisfied.Count == 0)
        {
            return;
        }

        output.AppendLine();
        output.AppendLine(
            $"Satisfied by target {result.Target}. Windows lock-in, supported here ({satisfied.Count}):");
        output.AppendLine(
            "  These would be migration cost only if you moved off Windows. Not counted above.");

        var groups = satisfied
            .GroupBy(f => f.Rule.Id)
            .Select(g => (Rule: g.First().Rule, Items: g.OrderBy(f => f.FilePath ?? f.ProjectPath, StringComparer.Ordinal).ThenBy(f => f.Line ?? 0).ToList()))
            .OrderBy(g => g.Rule.Id, StringComparer.Ordinal);

        foreach ((RuleMetadata rule, List<Finding> items) in groups)
        {
            string occurrences = items.Count == 1 ? string.Empty : $"  ({items.Count} occurrences)";
            output.AppendLine($"  • {rule.Id} {rule.Title}{occurrences}");
            foreach (Finding item in items)
            {
                output.AppendLine($"      {Location(item)}");
            }
        }
    }

    private static string Location(Finding finding)
    {
        string file = finding.FilePath ?? finding.ProjectPath;
        return finding.Line is { } line ? $"{file}:{line}" : file;
    }

    private static string Lower<TEnum>(TEnum value) where TEnum : struct, Enum =>
        value.ToString().ToLowerInvariant();
}
