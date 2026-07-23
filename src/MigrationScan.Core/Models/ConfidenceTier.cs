namespace MigrationScan.Core.Models;

/// <summary>
/// How a finding was detected, and therefore how much to trust it (spec §5).
/// </summary>
public enum ConfidenceTier
{
    /// <summary>Derived from project/config/solution files via XML parsing. No ambiguity.</summary>
    Certain,

    /// <summary>Derived from Roslyn syntax trees without a resolved compilation. Some false positives.</summary>
    Probable,

    /// <summary>Derived from the semantic model or compiled assemblies. Post-v1.</summary>
    Verified,
}
