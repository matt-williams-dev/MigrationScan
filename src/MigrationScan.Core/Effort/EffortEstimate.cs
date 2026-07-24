namespace MigrationScan.Core.Effort;

/// <summary>
/// A heuristic effort estimate expressed as a range of engineer-days, plus a count of
/// blocking issues that cannot be estimated until an architectural decision is made
/// (spec §7). These are planning aids derived from static analysis, not a quote.
/// </summary>
/// <param name="MinDays">Low end of the estimated range, in engineer-days.</param>
/// <param name="MaxDays">High end of the estimated range, in engineer-days.</param>
/// <param name="BlockerCount">Distinct blocking issues excluded from the day range.</param>
public sealed record EffortEstimate(double MinDays, double MaxDays, int BlockerCount)
{
    public static EffortEstimate Zero { get; } = new(0, 0, 0);

    public EffortEstimate Add(EffortEstimate other) =>
        new(MinDays + other.MinDays, MaxDays + other.MaxDays, BlockerCount + other.BlockerCount);
}
