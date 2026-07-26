using MigrationScan.Core.Analysis;
using MigrationScan.Core.Models;

namespace MigrationScan.Reporting;

/// <summary>
/// Turns locations into opaque ids so a report can be handed to a stranger without a review.
/// </summary>
/// <remarks>
/// Applied at the boundary — in the JSON writer — and nowhere else. The console, Markdown and
/// SARIF outputs keep full paths on purpose: SARIF exists to annotate a specific line in a
/// specific file, and a developer scanning their own solution already owns the information.
/// Stripping paths there would withhold what they know and make the CI story inert, without
/// protecting anybody.
///
/// What survives is chosen, not incidental. Rule ids, severities, tiers, effort bands and
/// occurrence counts are the entire pricing input. **Project names** survive because scope lines
/// grouped by project are what make a proposal readable. **Dependency identities** survive
/// because you cannot price a control from a vendor that folded without knowing which one it is.
/// Locations go; identities stay.
/// </remarks>
public static class Redaction
{
    /// <summary>A path replaced by its opaque id.</summary>
    public static string? Path(string? path) => path is null ? null : Fingerprints.FileId(path);

    /// <summary>
    /// A dependency's declared location. A COM type library is identified by its GUID, which is
    /// identity rather than location and is kept; a service endpoint keeps only its scheme and
    /// the fact that it exists; anything else is a path and becomes an opaque id.
    /// </summary>
    public static string? Source(string? source, ReferenceKind kind)
    {
        if (source is null) return null;

        // A COM GUID names the component, not where it lives — the same value on every machine
        // that ever registered it, and the only reliable way to identify one.
        if (kind == ReferenceKind.Com) return source;

        if (kind == ReferenceKind.WebService)
        {
            // Keep enough to say "there is an HTTPS endpoint here"; drop the host and path, which
            // is where the internal DNS names and route structure live.
            return Uri.TryCreate(source, UriKind.Absolute, out Uri? uri)
                ? $"{uri.Scheme}://<redacted>"
                : "<redacted>";
        }

        return Fingerprints.FileId(source);
    }

    /// <summary>
    /// A warning with its path removed from both the structured field and the sentence.
    /// </summary>
    /// <remarks>
    /// Warning text is the easiest disclosure to miss: it reads like tooling chatter and it
    /// embeds paths in prose. Each path is replaced by exact-string substitution of a value the
    /// warning declares, not by pattern-matching prose — a regex over free text would both miss
    /// real paths and mangle innocent sentences. A warning naming several projects declares all
    /// of them, so it survives redaction rather than being withheld for carrying more than one.
    /// </remarks>
    public static ScanWarning Warning(ScanWarning warning)
    {
        string message = warning.Message;

        // Longest first: where one path is a prefix of another, substituting the short one first
        // would leave the tail of the longer path sitting in the sentence.
        foreach (string path in PathsNamedBy(warning)
                     .OrderByDescending(p => p.Length)
                     .ThenBy(p => p, StringComparer.Ordinal))
        {
            message = message.Replace(path, Fingerprints.FileId(path), StringComparison.Ordinal);
        }

        return warning with { Message = message, Path = Path(warning.Path), MentionedPaths = [] };
    }

    /// <summary>
    /// A stand-in for a warning whose text still named a path after redaction. The warning is
    /// replaced rather than removed.
    /// </summary>
    /// <remarks>
    /// Dropping the warning entirely would be the one failure this whole design exists to
    /// prevent. The README tells readers to check the warnings before trusting a scan's coverage,
    /// so a redacted report that quietly loses "this project failed to load" invites somebody to
    /// scope against partial coverage with nothing on the page suggesting anything is missing.
    /// The text goes; the fact that a warning happened stays.
    /// </remarks>
    public static ScanWarning Withheld() => new(
        "A scan warning is withheld here because its text still named a file path after "
        + "redaction, and publishing it would defeat the redaction. Re-run with --include-paths "
        + "to read it in full.",
        Path: null);

    /// <summary>
    /// True when <paramref name="warning"/> still names a path after redaction, which is the
    /// signal to swap it for <see cref="Withheld"/>.
    /// </summary>
    /// <remarks>
    /// The check runs over the whole sentence rather than the declared paths, so free text the
    /// analyzer passed through (a parser's own error message, say) is caught as well.
    /// </remarks>
    public static bool StillNamesAPath(ScanWarning warning) =>
        warning.Message.Contains(".csproj", StringComparison.OrdinalIgnoreCase)
        || warning.Message.Contains(".vbproj", StringComparison.OrdinalIgnoreCase)
        || warning.Message.Contains(".sln", StringComparison.OrdinalIgnoreCase)
        || warning.Message.Contains('/')
        || warning.Message.Contains('\\');

    private static IEnumerable<string> PathsNamedBy(ScanWarning warning) =>
        (warning.Path is { Length: > 0 } path ? (string[])[path] : [])
            .Concat(warning.MentionedPaths.Where(p => p.Length > 0))
            .Distinct(StringComparer.Ordinal);
}
