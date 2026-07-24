using MigrationScan.Core.Models;

namespace MigrationScan.Core.Effort;

/// <summary>
/// Turns findings into heuristic effort estimates (spec §7).
///
/// Each effort band maps to a range of engineer-days. Repeated occurrences of the same
/// rule in a project are multiplied by an occurrence factor that flattens out — fifty
/// ConfigurationManager calls are not fifty times the work of one. Blocker-band findings
/// are not given a day range: they need an architectural decision first, so they are
/// counted separately.
/// </summary>
public static class EffortModel
{
    /// <summary>The engineer-day range for an effort band. Blocker has no range.</summary>
    public static (double Min, double Max) DayRange(EffortBand band) => band switch
    {
        EffortBand.Trivial => (0.1, 0.5),
        EffortBand.Small => (0.5, 2.0),
        EffortBand.Medium => (2.0, 5.0),
        EffortBand.Large => (5.0, 15.0),
        EffortBand.Blocker => (0.0, 0.0),
        _ => (0.0, 0.0),
    };

    /// <summary>
    /// Sub-linear multiplier for repeated occurrences of one rule, so effort grows with
    /// scale without scaling linearly with raw occurrence count.
    /// </summary>
    public static double OccurrenceFactor(int occurrences) => occurrences switch
    {
        <= 1 => 1.0,
        <= 5 => 1.5,
        <= 20 => 2.0,
        _ => 3.0,
    };

    /// <summary>
    /// Estimate for one project's findings. Findings satisfied by a Windows target are not
    /// migration cost and are excluded from the estimate.
    /// </summary>
    public static EffortEstimate ForFindings(IEnumerable<Finding> findings)
    {
        EffortEstimate estimate = EffortEstimate.Zero;

        // Group by rule: the occurrence factor applies per rule, and a blocking rule counts once.
        foreach (IGrouping<string, Finding> group in findings
            .Where(f => !f.SatisfiedByTarget)
            .GroupBy(f => f.Rule.Id))
        {
            EffortBand band = group.First().Rule.Effort;
            if (band == EffortBand.Blocker)
            {
                estimate = estimate.Add(new EffortEstimate(0, 0, 1));
                continue;
            }

            (double min, double max) = DayRange(band);
            double factor = OccurrenceFactor(group.Count());
            // Kept precise here; the reporter rounds for display so accumulated groups don't drift.
            estimate = estimate.Add(new EffortEstimate(min * factor, max * factor, 0));
        }

        return estimate;
    }

    /// <summary>Estimate for a single project within a result (by its repo-relative path).</summary>
    public static EffortEstimate ForProject(AnalysisResult result, string projectPath) =>
        ForFindings(result.Findings.Where(f => f.ProjectPath == projectPath));

    /// <summary>
    /// Solution total: the sum of the per-project estimates, so the occurrence factor stays
    /// scoped to each project rather than being applied across the whole solution.
    /// </summary>
    public static EffortEstimate ForSolution(AnalysisResult result) =>
        result.Findings
            .GroupBy(f => f.ProjectPath)
            .Select(ForFindings)
            .Aggregate(EffortEstimate.Zero, (total, project) => total.Add(project));

    /// <summary>Rounds engineer-days to one decimal place for stable, readable output.</summary>
    public static double Round(double days) => Math.Round(days, 1, MidpointRounding.AwayFromZero);
}
