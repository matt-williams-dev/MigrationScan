using MigrationScan.Core.Models;

namespace MigrationScan.Core.Analysis;

/// <summary>
/// Stable identity for a finding, used to match against a baseline (spec section 9,
/// <c>--baseline</c>). Deliberately excludes the line number so a finding survives unrelated
/// edits that shift lines; it keys on the rule, the file, and the message.
/// </summary>
public static class Fingerprints
{
    // Unit separator: unlikely to appear in any field, so joins are unambiguous.
    private const string Separator = "";

    public static string Of(Finding finding) =>
        Of(finding.Rule.Id, finding.FilePath ?? finding.ProjectPath, finding.Message);

    public static string Of(string ruleId, string file, string message) =>
        string.Join(Separator, ruleId, file, message);
}
