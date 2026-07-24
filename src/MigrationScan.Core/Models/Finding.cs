namespace MigrationScan.Core.Models;

/// <summary>
/// A single detected issue. Carries its rule's metadata (so severity/tier/effort
/// travel with it) plus the specific location and a human-readable message.
/// </summary>
/// <param name="Rule">The rule that produced this finding.</param>
/// <param name="Message">Specific, human-readable description of what was found here.</param>
/// <param name="ProjectPath">Repo-relative path of the owning project, forward-slashed.</param>
/// <param name="FilePath">Repo-relative path of the file the finding anchors to, or null.</param>
/// <param name="Line">1-based line number, or null when not line-specific.</param>
public sealed record Finding(
    RuleMetadata Rule,
    string Message,
    string ProjectPath,
    string? FilePath,
    int? Line)
{
    /// <summary>
    /// True when this is a Windows lock-in finding (its rule is <see cref="RulePlatform.Windows"/>)
    /// and the scan target is a Windows TFM, so the API still works and the finding is not
    /// migration cost. Such findings stay visible but are relabelled and excluded from the
    /// severity counts, effort rollup, and <c>--fail-on</c> threshold. Set during analysis.
    /// </summary>
    public bool SatisfiedByTarget { get; init; }
}
