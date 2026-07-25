using System.Globalization;
using System.Text;
using MigrationScan.Core.Effort;
using MigrationScan.Core.Models;

namespace MigrationScan.Reporting;

/// <summary>
/// Renders an <see cref="AnalysisResult"/> as a shareable Markdown report (spec §8, §13):
/// executive summary, blockers, findings by project, effort breakdown, remediation, and a
/// methodology/limitations section. Deterministic — no timestamps or machine-specific data.
/// </summary>
public static class MarkdownReportWriter
{
    private const string Disclaimer =
        "These figures are heuristic planning aids derived from static analysis and are not a quote.";

    public static string Write(AnalysisResult result)
    {
        StringBuilder md = new();

        WriteHeader(md, result);
        WriteExecutiveSummary(md, result);
        WriteNotAssessed(md, result);
        WriteSatisfiedByTarget(md, result);
        WriteWarnings(md, result);
        WriteBlockers(md, result);
        WriteFindingsByProject(md, result);
        WriteEffortBreakdown(md, result);
        WriteReferences(md, result);
        WriteRemediation(md, result);
        WriteMethodology(md);

        // Normalize to LF so output is byte-identical across operating systems (StringBuilder
        // .AppendLine emits Environment.NewLine, which is CRLF on Windows).
        string text = md.ToString().Replace("\r\n", "\n").Replace("\r", "\n");
        return text.TrimEnd() + "\n";
    }

    private static void WriteHeader(StringBuilder md, AnalysisResult result)
    {
        md.AppendLine("# .NET Framework Migration Assessment");
        md.AppendLine();
        md.AppendLine($"Static analysis of a solution's readiness to move to `{result.Target}`, produced by MigrationScan.");
        md.AppendLine();
    }

    private static void WriteExecutiveSummary(StringBuilder md, AnalysisResult result)
    {
        IReadOnlyDictionary<Severity, int> counts = result.CountsBySeverity();
        EffortEstimate effort = EffortModel.ForSolution(result);

        md.AppendLine("## Executive summary");
        md.AppendLine();
        md.AppendLine($"- **Projects scanned:** {result.Projects.Count}");
        md.AppendLine(
            $"- **Findings:** {result.ActiveFindings.Count()} " +
            $"(blocker {counts[Severity.Blocker]} · high {counts[Severity.High]} · " +
            $"medium {counts[Severity.Medium]} · low {counts[Severity.Low]})");
        md.AppendLine($"- **Estimated effort:** {FormatEffort(effort)}");
        int satisfiedCount = result.SatisfiedFindings.Count();
        if (satisfiedCount > 0)
        {
            md.AppendLine(
                $"- **Windows lock-in satisfied by `{result.Target}`:** {satisfiedCount} " +
                "(supported on this target — see below, not counted or estimated)");
        }

        if (result.NotAssessed.Count > 0)
        {
            md.AppendLine($"- **Projects not assessed:** {result.NotAssessed.Count} (see below — scope separately)");
        }

        int thirdPartyCount = result.DistinctThirdPartyCount();
        if (thirdPartyCount > 0)
        {
            md.AppendLine(
                $"- **Third-party references:** {thirdPartyCount} distinct " +
                "(see below — inventory, not counted or estimated)");
        }

        md.AppendLine();
        md.AppendLine($"> {Disclaimer}");
        md.AppendLine();
    }

    private static void WriteNotAssessed(StringBuilder md, AnalysisResult result)
    {
        if (result.NotAssessed.Count == 0)
        {
            return;
        }

        md.AppendLine("## Not assessed — scope separately");
        md.AppendLine();
        md.AppendLine(
            "These projects are part of the solution but are not C#/VB, so their contents were not " +
            "analyzed. They still need migration planning of their own and are **not** in the effort estimate:");
        md.AppendLine();
        md.AppendLine("| Project | Type | Location |");
        md.AppendLine("| --- | --- | --- |");
        foreach (NotAssessedProject project in result.NotAssessed)
        {
            md.AppendLine($"| {EscapeCell(project.Name)} | {EscapeCell(project.ProjectType)} | `{project.Path}` |");
        }

        md.AppendLine();
    }

    private static void WriteWarnings(StringBuilder md, AnalysisResult result)
    {
        if (result.Warnings.Count == 0)
        {
            return;
        }

        md.AppendLine("## Scan warnings");
        md.AppendLine();
        md.AppendLine("The following were skipped and are not reflected in the findings below:");
        md.AppendLine();
        foreach (ScanWarning warning in result.Warnings)
        {
            md.AppendLine($"- {EscapeInline(warning.Message)}");
        }

        md.AppendLine();
    }

    // Windows lock-in findings the current (Windows) target satisfies. Listed for completeness
    // so the scope isn't hidden, but explicitly out of the counts and effort.
    private static void WriteSatisfiedByTarget(StringBuilder md, AnalysisResult result)
    {
        List<Finding> satisfied = result.SatisfiedFindings.ToList();
        if (satisfied.Count == 0)
        {
            return;
        }

        md.AppendLine($"## Satisfied by target `{result.Target}`");
        md.AppendLine();
        md.AppendLine(
            "These are Windows lock-in APIs (COM, P/Invoke, Registry, WMI, …). They are fully " +
            $"supported when targeting `{result.Target}`, so they are **not** migration cost here and " +
            "are excluded from the findings, counts, and effort below. They would become work only if " +
            "the migration also had to run off Windows:");
        md.AppendLine();
        md.AppendLine("| Rule | Location | Detail |");
        md.AppendLine("| --- | --- | --- |");
        foreach (Finding finding in satisfied)
        {
            md.AppendLine($"| {RuleLink(finding.Rule)} | `{Location(finding)}` | {EscapeCell(finding.Message)} |");
        }

        md.AppendLine();
    }

    private static void WriteBlockers(StringBuilder md, AnalysisResult result)
    {
        List<Finding> blockers = result.ActiveFindings.Where(f => f.Rule.Severity == Severity.Blocker).ToList();

        md.AppendLine("## Blockers");
        md.AppendLine();

        if (blockers.Count == 0)
        {
            md.AppendLine("No blocking issues were found.");
            md.AppendLine();
            return;
        }

        md.AppendLine("These need an architectural decision before migration can proceed:");
        md.AppendLine();
        foreach (Finding finding in blockers)
        {
            md.AppendLine($"- {RuleLink(finding.Rule)} — `{Location(finding)}` — {EscapeInline(finding.Message)}");
        }

        md.AppendLine();
    }

    private static void WriteFindingsByProject(StringBuilder md, AnalysisResult result)
    {
        md.AppendLine("## Findings by project");
        md.AppendLine();

        IEnumerable<string> projectPaths = result.Projects
            .Select(p => p.Path)
            .OrderBy(p => p, StringComparer.Ordinal);

        foreach (string projectPath in projectPaths)
        {
            List<Finding> findings = result.ActiveFindings.Where(f => f.ProjectPath == projectPath).ToList();

            md.AppendLine($"### `{projectPath}`");
            md.AppendLine();

            if (findings.Count == 0)
            {
                md.AppendLine("No findings.");
                md.AppendLine();
                continue;
            }

            md.AppendLine($"Estimated effort: {FormatEffort(EffortModel.ForProject(result, projectPath))}");
            md.AppendLine();
            md.AppendLine("| Rule | Severity | Tier | Effort | Location | Issue |");
            md.AppendLine("| --- | --- | --- | --- | --- | --- |");
            foreach (Finding finding in findings)
            {
                md.AppendLine(
                    $"| {RuleLink(finding.Rule)} | {Title(finding.Rule.Severity)} | {Title(finding.Rule.Tier)} " +
                    $"| {Title(finding.Rule.Effort)} | `{Location(finding)}` | {EscapeCell(finding.Message)} |");
            }

            md.AppendLine();
        }
    }

    private static void WriteEffortBreakdown(StringBuilder md, AnalysisResult result)
    {
        md.AppendLine("## Effort breakdown");
        md.AppendLine();
        md.AppendLine("| Project | Findings | Estimated days | Needs decision |");
        md.AppendLine("| --- | --- | --- | --- |");

        foreach (string projectPath in result.Projects.Select(p => p.Path).OrderBy(p => p, StringComparer.Ordinal))
        {
            int findingCount = result.ActiveFindings.Count(f => f.ProjectPath == projectPath);
            EffortEstimate effort = EffortModel.ForProject(result, projectPath);
            md.AppendLine($"| `{projectPath}` | {findingCount} | {FormatDays(effort)} | {effort.BlockerCount} |");
        }

        EffortEstimate total = EffortModel.ForSolution(result);
        md.AppendLine($"| **Total** | **{result.ActiveFindings.Count()}** | **{FormatDays(total)}** | **{total.BlockerCount}** |");
        md.AppendLine();
        md.AppendLine($"_{Disclaimer}_");
        md.AppendLine();
    }

    // The dependency catalog. Deliberately separate from findings: most third-party references
    // are perfectly fine, but every one of them is a question a migration has to answer — does
    // this ship a modern .NET build, is the vendor still around, is there a successor?
    private static void WriteReferences(StringBuilder md, AnalysisResult result)
    {
        if (result.References.Count == 0)
        {
            return;
        }

        md.AppendLine("## References");
        md.AppendLine();
        md.AppendLine(
            "Everything the scanned projects declare a dependency on, read from the project files. " +
            "This is an inventory, not findings: nothing here is counted, estimated, or a build failure. " +
            "It's the list to research — check each third-party component for a supported .NET 10 release " +
            "before committing to a plan.");
        md.AppendLine();

        WriteThirdPartyReferences(md, result);
        WriteProjectReferences(md, result);
        WriteFrameworkReferenceNote(md, result);
    }

    private static void WriteThirdPartyReferences(StringBuilder md, AnalysisResult result)
    {
        // One row per distinct component across the solution — the same package referenced by
        // six projects is one thing to research, not six.
        // Case-insensitive on the name so "newtonsoft.json" and "Newtonsoft.Json" are one row.
        var groups = result.ThirdPartyReferences
            .GroupBy(r => (r.Kind, Key: r.Name.ToUpperInvariant()))
            .Select(g => new
            {
                g.First().Kind,
                g.First().Name,
                Versions = g.Select(r => r.Version).OfType<string>().Distinct(StringComparer.Ordinal)
                    .OrderBy(v => v, StringComparer.Ordinal).ToList(),
                Sources = g.Select(r => r.Source).OfType<string>().Distinct(StringComparer.Ordinal)
                    .OrderBy(s => s, StringComparer.Ordinal).ToList(),
                Projects = g.Select(r => r.ProjectPath).Distinct(StringComparer.Ordinal).Count(),
            })
            .OrderBy(g => g.Kind)
            .ThenBy(g => g.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        md.AppendLine($"### Third-party ({groups.Count} distinct)");
        md.AppendLine();

        if (groups.Count == 0)
        {
            md.AppendLine("None. Every reference is either a framework assembly or another project in this solution.");
            md.AppendLine();
            return;
        }

        md.AppendLine("| Reference | Kind | Version | Used by | Resolved from |");
        md.AppendLine("| --- | --- | --- | --- | --- |");
        foreach (var group in groups)
        {
            string versions = group.Versions.Count == 0 ? "—" : string.Join(", ", group.Versions);
            string sources = group.Sources.Count == 0 ? "—" : string.Join("<br>", group.Sources.Select(s => $"`{s}`"));
            string projects = group.Projects == 1 ? "1 project" : $"{group.Projects} projects";
            md.AppendLine(
                $"| {EscapeCell(group.Name)} | {KindLabel(group.Kind)} | {EscapeCell(versions)} | {projects} | {EscapeCell(sources)} |");
        }

        md.AppendLine();
    }

    private static void WriteProjectReferences(StringBuilder md, AnalysisResult result)
    {
        List<ReferenceRecord> projectReferences = result.References
            .Where(r => r.Kind == ReferenceKind.Project)
            .ToList();

        if (projectReferences.Count == 0)
        {
            return;
        }

        md.AppendLine("### Solution-internal project references");
        md.AppendLine();
        md.AppendLine("This solution's own code. Already in scope — listed to show the build order dependencies:");
        md.AppendLine();
        md.AppendLine("| Project | Depends on |");
        md.AppendLine("| --- | --- |");
        foreach (var group in projectReferences
            .GroupBy(r => r.ProjectPath, StringComparer.Ordinal)
            .OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            string dependencies = string.Join(", ", group
                .Select(r => r.Name)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase));
            md.AppendLine($"| `{group.Key}` | {EscapeCell(dependencies)} |");
        }

        md.AppendLine();
    }

    // Stated rather than silently dropped: the BCL is excluded from the table above on purpose.
    private static void WriteFrameworkReferenceNote(StringBuilder md, AnalysisResult result)
    {
        int frameworkCount = result.References.Count(r => r.IsFrameworkAssembly);
        if (frameworkCount == 0)
        {
            return;
        }

        string sentence = frameworkCount == 1
            ? "1 framework assembly reference was also read (`System.*`, `mscorlib`, WPF, …) and is not listed — " +
              "it moves with the runtime rather than needing research."
            : $"{frameworkCount} framework assembly references were also read (`System.*`, `mscorlib`, WPF, …) and are " +
              "not listed — they move with the runtime rather than needing research.";

        md.AppendLine($"_{sentence}_");
        md.AppendLine();
    }

    private static string KindLabel(ReferenceKind kind) => kind switch
    {
        ReferenceKind.Package => "NuGet package",
        ReferenceKind.Assembly => "GAC assembly",
        ReferenceKind.VendoredAssembly => "Vendored DLL",
        ReferenceKind.Com => "COM / ActiveX",
        ReferenceKind.Project => "Project",
        ReferenceKind.WebService => "Service proxy",
        _ => kind.ToString(),
    };

    private static void WriteRemediation(StringBuilder md, AnalysisResult result)
    {
        List<RuleMetadata> rules = result.ActiveFindings
            .Select(f => f.Rule)
            .DistinctBy(r => r.Id)
            .OrderBy(r => r.Id, StringComparer.Ordinal)
            .ToList();

        if (rules.Count == 0)
        {
            return;
        }

        md.AppendLine("## Remediation guidance");
        md.AppendLine();
        foreach (RuleMetadata rule in rules)
        {
            md.AppendLine($"**{RuleLink(rule)} — {EscapeInline(rule.Title)}**");
            md.AppendLine();
            md.AppendLine(EscapeInline(rule.Remediation));
            md.AppendLine();
        }
    }

    private static void WriteMethodology(StringBuilder md)
    {
        md.AppendLine("## Methodology & limitations");
        md.AppendLine();
        md.AppendLine(
            "MigrationScan parses `.sln` and `.csproj` files as XML and reads `.cs` files with Roslyn — " +
            "no MSBuild or Visual Studio required, and no source code leaves the machine. Every finding " +
            "carries a **confidence tier**:");
        md.AppendLine();
        md.AppendLine("- **Tier 1 — Certain:** read directly from project, config, or solution files.");
        md.AppendLine("- **Tier 2 — Probable:** matched on the syntax tree without a resolved compilation, so some may be false positives.");
        md.AppendLine();
        md.AppendLine(
            "Effort figures apply a per-rule range and a flattening occurrence factor, aggregated per project " +
            "and across the solution. Two things are tracked separately and can differ: **severity** (the " +
            "*Blockers* section lists the highest-impact findings) and **estimability** (the *Needs decision* " +
            "count is the subset whose effort is unbounded until an architectural decision is made). A finding " +
            "can be a severity blocker yet still estimable — for example replacing `BinaryFormatter` is high " +
            "impact but a bounded change.");
        md.AppendLine();
        md.AppendLine($"_{Disclaimer}_");
    }

    // --- formatting helpers ---

    private static string FormatEffort(EffortEstimate effort)
    {
        string days = FormatDays(effort);
        if (effort.BlockerCount == 0)
        {
            return $"{days} engineer-days";
        }

        // Deliberately avoids the word "blocker" here: this is the *estimability* dimension
        // (effort band), distinct from severity-Blocker findings listed under "## Blockers".
        string decisions = $"{effort.BlockerCount} item{(effort.BlockerCount == 1 ? "" : "s")} " +
            "requiring an architectural decision before they can be estimated";
        return effort is { MinDays: 0, MaxDays: 0 }
            ? decisions
            : $"{days} engineer-days, plus {decisions}";
    }

    private static string FormatDays(EffortEstimate effort)
    {
        double min = EffortModel.Round(effort.MinDays);
        double max = EffortModel.Round(effort.MaxDays);
        return min == 0 && max == 0 ? "—" : $"{Number(min)}–{Number(max)}";
    }

    private static string Number(double value) => value.ToString("0.#", CultureInfo.InvariantCulture);

    private static string RuleLink(RuleMetadata rule) => $"[{rule.Id}]({rule.DocsUrl})";

    private static string Location(Finding finding)
    {
        string file = finding.FilePath ?? finding.ProjectPath;
        return finding.Line is { } line ? $"{file}:{line}" : file;
    }

    private static string Title<TEnum>(TEnum value) where TEnum : struct, Enum
    {
        string text = value.ToString();
        return string.Concat(char.ToUpperInvariant(text[0]), text[1..].ToLowerInvariant());
    }

    // Escape content used inside a Markdown table cell.
    private static string EscapeCell(string text) => EscapeInline(text).Replace("|", "\\|");

    // Neutralize characters that would otherwise be interpreted as Markdown/HTML.
    private static string EscapeInline(string text) => text
        .Replace("\r", " ")
        .Replace("\n", " ")
        .Replace("<", "&lt;")
        .Replace(">", "&gt;");
}
