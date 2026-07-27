using System.Globalization;
using MigrationScan.Core.Effort;

namespace MigrationScan.Reporting;

/// <summary>
/// Effort ranges as a reader sees them, in one place so the console and the Markdown report
/// cannot drift into quoting the same estimate two different ways.
/// </summary>
/// <remarks>
/// Rounding happens here rather than in the model: <see cref="EffortModel"/> accumulates precise
/// values so summing many groups does not compound rounding error, and display rounds once at the
/// end.
/// </remarks>
public static class EffortFormat
{
    /// <summary>The day range alone, for example <c>29.3–88</c>, or <c>n/a</c> when there is none.</summary>
    public static string Days(EffortEstimate effort)
    {
        double min = EffortModel.Round(effort.MinDays);
        double max = EffortModel.Round(effort.MaxDays);
        return min == 0 && max == 0 ? "n/a" : $"{Number(min)}–{Number(max)}";
    }

    /// <summary>
    /// The day range with its unit, for example <c>29.3–88 engineer-days</c>.
    /// </summary>
    /// <remarks>
    /// Two different situations produce no range, and telling a reader the wrong one wastes their
    /// afternoon. An estate with nothing to fix has no work to price. An estate whose every
    /// finding needs an architectural decision has work nobody can price yet.
    /// </remarks>
    public static string DaysWithUnit(EffortEstimate effort) => effort switch
    {
        { MinDays: 0, MaxDays: 0, BlockerCount: 0 } => "none",
        { MinDays: 0, MaxDays: 0 } => "not yet estimable",
        _ => $"{Days(effort)} engineer-days",
    };

    /// <summary>
    /// The same range with a short unit, for a line where <c>engineer-days</c> has already been
    /// spelled out nearby. Saves the dozen columns that decide whether a row wraps at 80.
    /// </summary>
    public static string DaysShort(EffortEstimate effort) => effort switch
    {
        { MinDays: 0, MaxDays: 0, BlockerCount: 0 } => "nothing to price",
        { MinDays: 0, MaxDays: 0 } => "nothing priceable yet",
        _ => $"{Days(effort)} days",
    };

    private static string Number(double value) => value.ToString("0.#", CultureInfo.InvariantCulture);
}
