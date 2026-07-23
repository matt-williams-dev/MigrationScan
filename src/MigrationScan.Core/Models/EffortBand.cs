namespace MigrationScan.Core.Models;

/// <summary>
/// Heuristic effort band for a single finding (spec §7). These are planning aids
/// derived from static analysis, not a quote.
/// </summary>
public enum EffortBand
{
    /// <summary>Under 0.5 day. Mechanical, often a find-and-replace.</summary>
    Trivial,

    /// <summary>0.5 to 2 days. Localized change, low risk.</summary>
    Small,

    /// <summary>2 to 5 days. Touches multiple files, needs testing.</summary>
    Medium,

    /// <summary>5 to 15 days. Subsystem rework.</summary>
    Large,

    /// <summary>Unbounded. Needs an architectural decision before estimating.</summary>
    Blocker,
}
