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
}
