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

    /// <summary>Count of findings at each severity, including zero-count severities.</summary>
    public IReadOnlyDictionary<Severity, int> CountsBySeverity()
    {
        Dictionary<Severity, int> counts = new()
        {
            [Severity.Blocker] = 0,
            [Severity.High] = 0,
            [Severity.Medium] = 0,
            [Severity.Low] = 0,
        };

        foreach (Finding finding in Findings)
        {
            counts[finding.Rule.Severity]++;
        }

        return counts;
    }

    /// <summary>
    /// True if any finding is at least as severe as <paramref name="threshold"/>. Drives the
    /// <c>--fail-on</c> exit code. Severity is ordered most-severe-first, so "at or above" is
    /// a numeric &lt;= comparison.
    /// </summary>
    public bool FailsThreshold(Severity threshold) =>
        Findings.Any(f => f.Rule.Severity <= threshold);
}
