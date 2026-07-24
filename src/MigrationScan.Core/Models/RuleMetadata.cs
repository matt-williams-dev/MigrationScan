namespace MigrationScan.Core.Models;

/// <summary>
/// Static metadata for a rule, loaded from JSON data (spec §6, §14). The C# rule
/// implementations hold detection logic only; everything descriptive lives here.
/// Rule IDs are stable and never reused.
/// </summary>
public sealed record RuleMetadata(
    string Id,
    string Title,
    string Category,
    Severity Severity,
    EffortBand Effort,
    ConfidenceTier Tier,
    string Remediation,
    string DocsUrl)
{
    /// <summary>
    /// Whether this rule's findings are a problem on any target or only when moving off
    /// Windows. Defaults to <see cref="RulePlatform.Any"/>; set to <see cref="RulePlatform.Windows"/>
    /// in the catalog for Windows lock-in rules. Init-only with a default so it is optional in
    /// the JSON catalog and existing construction sites keep compiling.
    /// </summary>
    public RulePlatform Platform { get; init; } = RulePlatform.Any;
}
