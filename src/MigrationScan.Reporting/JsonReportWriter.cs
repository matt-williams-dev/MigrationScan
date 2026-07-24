using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using MigrationScan.Core.Effort;
using MigrationScan.Core.Models;

namespace MigrationScan.Reporting;

/// <summary>
/// Writes an <see cref="AnalysisResult"/> as JSON against a stable, versioned schema
/// (spec §8). Output is deterministic: fixed key order, no timestamps, invariant formatting.
/// </summary>
public static class JsonReportWriter
{
    /// <summary>
    /// Schema version. 1.1 added the effort rollup; 1.2 added the `notAssessed` array and
    /// `summary.projectsNotAssessed`; 1.3 added portability awareness — `finding.platform`,
    /// `finding.satisfiedByTarget`, and `summary.windowsLockInSatisfied`. `summary.totalFindings`
    /// and `project.findingCount` count only active findings (a Windows target's satisfied
    /// lock-in findings are excluded). All additive, backward-compatible over 1.0.
    /// </summary>
    public const string SchemaVersion = "1.3";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        // CLI/file output, not HTML — keep <, >, ', & literal instead of \uXXXX-escaped.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        // Nulls (e.g. an absent line number) are omitted for a cleaner document.
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string Write(AnalysisResult result)
    {
        IReadOnlyDictionary<Severity, int> counts = result.CountsBySeverity();
        int satisfiedCount = result.SatisfiedFindings.Count();

        ReportDocument document = new(
            SchemaVersion: SchemaVersion,
            Target: result.Target,
            Summary: new ReportSummary(
                ProjectsScanned: result.Projects.Count,
                TotalFindings: result.ActiveFindings.Count(),
                FindingsBySeverity: new SeverityCounts(
                    Blocker: counts[Severity.Blocker],
                    High: counts[Severity.High],
                    Medium: counts[Severity.Medium],
                    Low: counts[Severity.Low]),
                Effort: ToEffort(EffortModel.ForSolution(result)),
                ProjectsNotAssessed: result.NotAssessed.Count,
                // Omitted entirely on a cross-platform target (nothing is satisfied).
                WindowsLockInSatisfied: satisfiedCount == 0 ? null : satisfiedCount),
            Projects: result.Projects
                .OrderBy(p => p.Path, StringComparer.Ordinal)
                .Select(p => new ReportProject(
                    Path: p.Path,
                    FindingCount: result.ActiveFindings.Count(f => f.ProjectPath == p.Path),
                    Effort: ToEffort(EffortModel.ForProject(result, p.Path))))
                .ToList(),
            Findings: result.Findings.Select(ToDto).ToList(),
            NotAssessed: result.NotAssessed
                .Select(p => new ReportNotAssessed(p.Name, p.Path, p.ProjectType, p.Reason))
                .ToList(),
            Warnings: result.Warnings.Select(w => new ReportWarning(w.Message, w.Path)).ToList());

        // Normalize indentation newlines to LF for byte-identical output across operating
        // systems. Newlines inside string values are escaped by the serializer, so only
        // formatting newlines are affected.
        return JsonSerializer.Serialize(document, SerializerOptions).Replace("\r\n", "\n");
    }

    // Effort as heuristic engineer-day ranges, rounded for display; "needsDecision" is the
    // count of blocking issues that need an architectural decision before they can be estimated.
    private static ReportEffort ToEffort(EffortEstimate estimate) => new(
        MinDays: EffortModel.Round(estimate.MinDays),
        MaxDays: EffortModel.Round(estimate.MaxDays),
        NeedsDecision: estimate.BlockerCount);

    private static ReportFinding ToDto(Finding finding) => new(
        RuleId: finding.Rule.Id,
        Title: finding.Rule.Title,
        Category: finding.Rule.Category,
        Severity: finding.Rule.Severity,
        Tier: finding.Rule.Tier,
        Effort: finding.Rule.Effort,
        Message: finding.Message,
        Project: finding.ProjectPath,
        File: finding.FilePath,
        Line: finding.Line,
        Remediation: finding.Rule.Remediation,
        DocsUrl: finding.Rule.DocsUrl,
        // Emitted only for Windows lock-in rules; omitted for the ordinary "any" case.
        Platform: finding.Rule.Platform == RulePlatform.Windows ? "windows" : null,
        // True only when a Windows target satisfies this lock-in finding; otherwise omitted.
        SatisfiedByTarget: finding.SatisfiedByTarget ? true : null);

    private sealed record ReportDocument(
        string SchemaVersion,
        string Target,
        ReportSummary Summary,
        IReadOnlyList<ReportProject> Projects,
        IReadOnlyList<ReportFinding> Findings,
        IReadOnlyList<ReportNotAssessed> NotAssessed,
        IReadOnlyList<ReportWarning> Warnings);

    private sealed record ReportWarning(string Message, string? Path);

    private sealed record ReportNotAssessed(string Name, string Path, string ProjectType, string Reason);

    private sealed record ReportSummary(
        int ProjectsScanned,
        int TotalFindings,
        SeverityCounts FindingsBySeverity,
        ReportEffort Effort,
        int ProjectsNotAssessed,
        int? WindowsLockInSatisfied);

    private sealed record ReportProject(string Path, int FindingCount, ReportEffort Effort);

    private sealed record ReportEffort(double MinDays, double MaxDays, int NeedsDecision);

    private sealed record SeverityCounts(int Blocker, int High, int Medium, int Low);

    private sealed record ReportFinding(
        string RuleId,
        string Title,
        string Category,
        Severity Severity,
        ConfidenceTier Tier,
        EffortBand Effort,
        string Message,
        string Project,
        string? File,
        int? Line,
        string Remediation,
        string DocsUrl,
        string? Platform,
        bool? SatisfiedByTarget);
}
