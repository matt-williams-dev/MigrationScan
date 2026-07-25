using System.Security.Cryptography;
using System.Text;
using MigrationScan.Core.Models;

namespace MigrationScan.Core.Analysis;

/// <summary>
/// Stable identity for a finding, used to match against a baseline (spec §9, <c>--baseline</c>),
/// and the opaque ids that stand in for file paths in a redacted report.
/// </summary>
/// <remarks>
/// Deliberately excludes the line number so a finding survives unrelated edits that shift lines;
/// it keys on the rule, the file, and the message.
///
/// The result is a hash rather than the joined fields, for two reasons. A fingerprint is written
/// into the report, and a plain join would carry the very path the report set out not to disclose.
/// And it lets a fingerprint be compared across a redacted and an unredacted report of the same
/// solution — a client's baseline still matches, which it would not if one side keyed on a path
/// and the other on a hash of it.
///
/// The input to the hash is therefore load-bearing across releases: changing it invalidates every
/// committed baseline. No salt, no timestamp, no machine-specific input — the same solution must
/// fingerprint identically on any machine, forever.
/// </remarks>
public static class Fingerprints
{
    // Unit separator: cannot appear in a rule id, path or message, so the join is unambiguous
    // and "ab" can never collide with "a" + "b".
    private const char Separator = '';

    /// <summary>Length of an emitted id, in hex characters. 128 bits of a SHA-256 digest.</summary>
    private const int HexLength = 32;

    public static string Of(Finding finding) =>
        Of(finding.Rule.Id, finding.FilePath ?? finding.ProjectPath, finding.Message);

    public static string Of(string ruleId, string file, string message) =>
        Hash(string.Join(Separator, ruleId, file, message));

    /// <summary>
    /// The opaque id that replaces a file path in a redacted report. Stable for a given
    /// repo-relative path, so a consumer can still see that seven findings share a file without
    /// being told which file — which is real signal for effort, and costs no disclosure.
    /// </summary>
    public static string FileId(string path) => "f:" + Hash(path)[..16];

    private static string Hash(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..HexLength];
}
