using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using MigrationScan.Core.Analysis;
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
    /// `finding.satisfiedByTarget`, and `summary.windowsLockInSatisfied`; 1.4 added the
    /// `references` inventory and `summary.thirdPartyReferences`; 1.5 added the `targets`
    /// array, which carries both portability stances in one document; 1.6 added
    /// `finding.fingerprint`, `redacted`, and `finding.fileId` — which replaces (never
    /// redefines) `file` when paths are redacted.
    /// `summary.totalFindings` and `project.findingCount` count only active findings (a
    /// Windows target's satisfied lock-in findings are excluded). All additive,
    /// backward-compatible over 1.0.
    /// </summary>
    public const string SchemaVersion = "1.6";

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

    /// <summary>
    /// Writes the report. Paths are redacted by default: this is the document that leaves the
    /// machine, and the safe form has to be the one nobody has to know to ask for. Pass
    /// <paramref name="includePaths"/> to keep them, which is what <c>--include-paths</c> does.
    /// </summary>
    public static string Write(AnalysisResult result, bool includePaths = false)
    {
        (string crossPlatform, string windows) = TargetPlatform.Stances(result.Target);

        ReportDocument document = new(
            SchemaVersion: SchemaVersion,
            Target: result.Target,
            // Stated in the document rather than inferred from the shape of the fields, so a
            // consumer never has to guess whether an absent `file` means "redacted" or
            // "this finding has no file".
            Redacted: includePaths ? null : true,
            // Omitted entirely when the result was not produced by a scan, so a hand-built
            // report never claims a provenance it does not have.
            Scan: result.Provenance is { } p ? new ReportScan(p.ToolVersion, p.Commit) : null,
            Summary: Summarize(result),
            Projects: ProjectRollup(result),
            // Both portability stances, from the one analysis. The target affects only whether a
            // Windows lock-in finding counts as cost, so the alternate stance is an exact
            // re-evaluation rather than a second scan — which is why the customer runs the tool once.
            Targets:
            [
                ToTarget(result.ForTarget(crossPlatform), "crossPlatform", result.Target),
                ToTarget(result.ForTarget(windows), "windows", result.Target),
            ],
            Findings: result.Findings.Select(f => ToDto(f, includePaths)).ToList(),
            // Project paths are kept even when redacting: they are the project's identity, and
            // grouping scope lines by project is what makes a downstream proposal readable.
            NotAssessed: result.NotAssessed
                .Select(p => new ReportNotAssessed(p.Name, p.Path, p.ProjectType, p.Reason))
                .ToList(),
            // Flat and per-project rather than pre-grouped: it's the lossless form, and a
            // consumer wanting a solution-wide roll-up can group on (kind, name) itself.
            References: result.References.Select(r => ToDto(r, includePaths)).ToList(),
            Warnings: Warnings(result, includePaths));

        // Normalize indentation newlines to LF for byte-identical output across operating
        // systems. Newlines inside string values are escaped by the serializer, so only
        // formatting newlines are affected.
        return JsonSerializer.Serialize(document, SerializerOptions).Replace("\r\n", "\n");
    }

    /// <summary>
    /// One portability stance: the counts and effort that hold if the migration is assessed
    /// against <paramref name="view"/>'s target. The findings themselves are not repeated —
    /// a stance satisfies exactly the findings whose <c>platform</c> matches its
    /// <c>satisfiedPlatform</c>, so a consumer derives the active set rather than reconciling
    /// two copies of the same array.
    /// </summary>
    private static ReportTarget ToTarget(AnalysisResult view, string stance, string documentTarget) => new(
        Target: view.Target,
        Stance: stance,
        // Marks the stance the top-level target/summary/projects describe, so a consumer that
        // reads only the document root knows which one it got.
        Default: view.Target == documentTarget ? true : null,
        SatisfiedPlatform: TargetPlatform.IsWindows(view.Target) ? "windows" : null,
        Summary: Summarize(view),
        Projects: ProjectRollup(view));

    private static ReportSummary Summarize(AnalysisResult result)
    {
        IReadOnlyDictionary<Severity, int> counts = result.CountsBySeverity();
        int satisfiedCount = result.SatisfiedFindings.Count();

        return new ReportSummary(
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
            WindowsLockInSatisfied: satisfiedCount == 0 ? null : satisfiedCount,
            // Distinct components, not declaration sites — the `references` array is
            // per-project and will be longer.
            ThirdPartyReferences: result.DistinctThirdPartyCount());
    }

    private static IReadOnlyList<ReportProject> ProjectRollup(AnalysisResult result) =>
        result.Projects
            .OrderBy(p => p.Path, StringComparer.Ordinal)
            .Select(p => new ReportProject(
                Path: p.Path,
                FindingCount: result.ActiveFindings.Count(f => f.ProjectPath == p.Path),
                Effort: ToEffort(EffortModel.ForProject(result, p.Path))))
            .ToList();

    // Effort as heuristic engineer-day ranges, rounded for display; "needsDecision" is the
    // count of blocking issues that need an architectural decision before they can be estimated.
    private static ReportEffort ToEffort(EffortEstimate estimate) => new(
        MinDays: EffortModel.Round(estimate.MinDays),
        MaxDays: EffortModel.Round(estimate.MaxDays),
        NeedsDecision: estimate.BlockerCount);

    private static ReportFinding ToDto(Finding finding, bool includePaths) => new(
        RuleId: finding.Rule.Id,
        Title: finding.Rule.Title,
        Category: finding.Rule.Category,
        Severity: finding.Rule.Severity,
        Tier: finding.Rule.Tier,
        Effort: finding.Rule.Effort,
        Message: finding.Message,
        Project: finding.ProjectPath,
        // `file` and `fileId` are alternatives, never both. Redefining what `file` *means* would
        // break every consumer that resolves it against a repo — a major bump, which downstream
        // readers refuse. `file` is already optional, so its absence is a shape consumers handle.
        File: includePaths ? finding.FilePath : null,
        FileId: includePaths ? null : Redaction.Path(finding.FilePath),
        Line: finding.Line,
        // Computed from the unredacted finding either way, which is what lets a baseline captured
        // from a redacted report still match an unredacted scan of the same solution.
        Fingerprint: Fingerprints.Of(finding),
        Remediation: finding.Rule.Remediation,
        DocsUrl: finding.Rule.DocsUrl,
        // Emitted only for Windows lock-in rules; omitted for the ordinary "any" case.
        Platform: finding.Rule.Platform == RulePlatform.Windows ? "windows" : null,
        // True only when a Windows target satisfies this lock-in finding; otherwise omitted.
        SatisfiedByTarget: finding.SatisfiedByTarget ? true : null);

    private static ReportReference ToDto(ReferenceRecord reference, bool includePaths) => new(
        Kind: reference.Kind,
        // Name and version are identity, not location, and survive redaction on purpose: a
        // vendored control cannot be researched, priced or replaced without knowing which it is.
        Name: reference.Name,
        Version: reference.Version,
        Source: includePaths ? reference.Source : Redaction.Source(reference.Source, reference.Kind),
        IsFrameworkAssembly: reference.IsFrameworkAssembly,
        IsThirdParty: reference.IsThirdParty,
        Project: reference.ProjectPath,
        DeclaredIn: includePaths ? reference.DeclaredIn : Fingerprints.FileId(reference.DeclaredIn),
        Line: reference.Line);

    /// <summary>
    /// Warnings, with paths removed from the prose as well as the field. One that still names a
    /// path afterwards is replaced by a placeholder, never removed: a redacted report carries the
    /// same number of warnings as an unredacted one, so nobody reads a short list as a clean scan.
    /// </summary>
    private static IReadOnlyList<ReportWarning> Warnings(AnalysisResult result, bool includePaths)
    {
        if (includePaths)
        {
            return result.Warnings.Select(w => new ReportWarning(w.Message, w.Path)).ToList();
        }

        return result.Warnings
            .Select(Redaction.Warning)
            .Select(w => Redaction.StillNamesAPath(w) ? Redaction.Withheld() : w)
            .Select(w => new ReportWarning(w.Message, w.Path))
            .ToList();
    }

    private sealed record ReportDocument(
        string SchemaVersion,
        string Target,
        bool? Redacted,
        ReportScan? Scan,
        ReportSummary Summary,
        IReadOnlyList<ReportProject> Projects,
        IReadOnlyList<ReportTarget> Targets,
        IReadOnlyList<ReportFinding> Findings,
        IReadOnlyList<ReportNotAssessed> NotAssessed,
        IReadOnlyList<ReportReference> References,
        IReadOnlyList<ReportWarning> Warnings);

    private sealed record ReportReference(
        ReferenceKind Kind,
        string Name,
        string? Version,
        string? Source,
        bool IsFrameworkAssembly,
        bool IsThirdParty,
        string Project,
        string DeclaredIn,
        int? Line);

    private sealed record ReportScan(string ToolVersion, string? Commit);

    private sealed record ReportTarget(
        string Target,
        string Stance,
        bool? Default,
        string? SatisfiedPlatform,
        ReportSummary Summary,
        IReadOnlyList<ReportProject> Projects);

    private sealed record ReportWarning(string Message, string? Path);

    private sealed record ReportNotAssessed(string Name, string Path, string ProjectType, string Reason);

    private sealed record ReportSummary(
        int ProjectsScanned,
        int TotalFindings,
        SeverityCounts FindingsBySeverity,
        ReportEffort Effort,
        int ProjectsNotAssessed,
        int? WindowsLockInSatisfied,
        int ThirdPartyReferences);

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
        string? FileId,
        int? Line,
        string Fingerprint,
        string Remediation,
        string DocsUrl,
        string? Platform,
        bool? SatisfiedByTarget);
}
