namespace MigrationScan.Core.Models;

/// <summary>
/// How badly a finding affects a migration. Also drives the <c>--fail-on</c> threshold.
/// Ordered most-severe first so numeric comparison works for thresholds.
/// </summary>
public enum Severity
{
    /// <summary>Needs an architectural decision before the migration can proceed.</summary>
    Blocker,

    /// <summary>Significant, likely to break the build or runtime without rework.</summary>
    High,

    /// <summary>Localized rework, needs testing.</summary>
    Medium,

    /// <summary>Minor or mechanical.</summary>
    Low,
}
