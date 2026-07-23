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
    string DocsUrl);
