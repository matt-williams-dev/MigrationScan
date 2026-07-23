namespace MigrationScan.Core.Models;

/// <summary>
/// The complete result of a scan: what was targeted, which projects were discovered,
/// and every finding. Findings are already in deterministic order.
/// </summary>
public sealed record AnalysisResult(
    string Target,
    IReadOnlyList<DiscoveredProject> Projects,
    IReadOnlyList<Finding> Findings)
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
