namespace MigrationScan.Core.Models;

/// <summary>
/// The complete result of a scan: what was targeted, which projects were discovered,
/// every finding, and any non-fatal warnings. Findings and warnings are already in
/// deterministic order.
/// </summary>
public sealed record AnalysisResult(
    string Target,
    IReadOnlyList<DiscoveredProject> Projects,
    IReadOnlyList<Finding> Findings,
    IReadOnlyList<ScanWarning> Warnings)
{
    /// <summary>
    /// Projects the scan could not analyze (non-C#/VB, e.g. SQL or deployment projects). Not
    /// findings — explicit scoping inputs, surfaced so coverage isn't silently overstated.
    /// </summary>
    public IReadOnlyList<NotAssessedProject> NotAssessed { get; init; } = [];

    /// <summary>
    /// Every dependency each project declares — packages, assemblies, COM/ActiveX interop,
    /// project references, service proxies. Inventory, not findings: it carries no severity or
    /// effort and is excluded from the counts, the estimate, and <c>--fail-on</c>. Its purpose
    /// is to make the third-party surface researchable before scoping.
    /// </summary>
    public IReadOnlyList<ReferenceRecord> References { get; init; } = [];

    /// <summary>
    /// References external to both the framework and this solution — the subset worth
    /// researching. Still one entry per declaring project; deduplicate for a solution-wide view.
    /// </summary>
    public IEnumerable<ReferenceRecord> ThirdPartyReferences => References.Where(r => r.IsThirdParty);

    /// <summary>
    /// Distinct third-party components across the solution. The same package referenced by six
    /// projects is one thing to research, so it counts once. Names are compared case-insensitively
    /// (<c>newtonsoft.json</c> and <c>Newtonsoft.Json</c> are the same component).
    /// </summary>
    public int DistinctThirdPartyCount() =>
        ThirdPartyReferences.DistinctBy(r => (r.Kind, r.Name.ToUpperInvariant())).Count();

    /// <summary>
    /// Findings that are actual migration cost for this target — everything except Windows
    /// lock-in findings satisfied by a Windows TFM. This is what the counts, effort rollup,
    /// and <c>--fail-on</c> threshold operate on.
    /// </summary>
    public IEnumerable<Finding> ActiveFindings => Findings.Where(f => !f.SatisfiedByTarget);

    /// <summary>
    /// Windows lock-in findings that the current (Windows) target satisfies. Still reported,
    /// but downgraded: not counted, not estimated, not a <c>--fail-on</c> trigger.
    /// </summary>
    public IEnumerable<Finding> SatisfiedFindings => Findings.Where(f => f.SatisfiedByTarget);

    /// <summary>
    /// Count of active findings at each severity, including zero-count severities. Findings
    /// satisfied by a Windows target are excluded — they are not migration cost.
    /// </summary>
    public IReadOnlyDictionary<Severity, int> CountsBySeverity()
    {
        Dictionary<Severity, int> counts = new()
        {
            [Severity.Blocker] = 0,
            [Severity.High] = 0,
            [Severity.Medium] = 0,
            [Severity.Low] = 0,
        };

        foreach (Finding finding in ActiveFindings)
        {
            counts[finding.Rule.Severity]++;
        }

        return counts;
    }

    /// <summary>
    /// True if any active finding is at least as severe as <paramref name="threshold"/>. Drives
    /// the <c>--fail-on</c> exit code. Severity is ordered most-severe-first, so "at or above"
    /// is a numeric &lt;= comparison. Findings satisfied by the target do not trip the threshold.
    /// </summary>
    public bool FailsThreshold(Severity threshold) =>
        ActiveFindings.Any(f => f.Rule.Severity <= threshold);
}
