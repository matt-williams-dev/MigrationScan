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
    /// embeds a path in prose. The path is replaced by exact-string substitution of the value the
    /// warning already carries, not by pattern-matching prose — a regex over free text would both
    /// miss real paths and mangle innocent sentences.
    /// </remarks>
    public static ScanWarning Warning(ScanWarning warning)
    {
        string message = warning.Message;

        if (warning.Path is { Length: > 0 } path)
        {
            message = message.Replace(path, Fingerprints.FileId(path), StringComparison.Ordinal);
        }

        return new ScanWarning(message, Path(warning.Path));
    }

    /// <summary>
    /// True when <paramref name="warning"/> still names a path after redaction — a warning that
    /// lists several paths in one sentence cannot be scrubbed by substituting the single path it
    /// carries. Such a warning is dropped rather than published half-redacted.
    /// </summary>
    /// <remarks>
    /// Errs towards dropping. Losing a warning costs the reader a little context; publishing one
    /// that still names a directory tree costs exactly what redaction exists to prevent.
    /// </remarks>
    public static bool StillNamesAPath(ScanWarning warning) =>
        warning.Message.Contains(".csproj", StringComparison.OrdinalIgnoreCase)
        || warning.Message.Contains(".vbproj", StringComparison.OrdinalIgnoreCase)
        || warning.Message.Contains(".sln", StringComparison.OrdinalIgnoreCase)
        || warning.Message.Contains('/')
        || warning.Message.Contains('\\');
}
