using System.Reflection;

namespace MigrationScan.Core.Models;

/// <summary>
/// What produced a report, and from which revision.
/// </summary>
/// <remarks>
/// Deliberately carries no timestamp. A report is byte-identical for the same input by design —
/// that is what makes it diffable and baselineable — and a clock would destroy that for the sake
/// of a fact the file's own modified date already carries. The commit is the honest answer to
/// "is this scan stale?": it identifies the exact revision assessed, months later, without
/// depending on when the file happened to be written.
/// </remarks>
/// <param name="ToolVersion">The MigrationScan version that produced the report.</param>
/// <param name="Commit">Commit the scanned tree was checked out at, when it is a git working tree.</param>
public sealed record ScanProvenance(string ToolVersion, string? Commit)
{
    /// <summary>The running tool's version, from its informational version attribute.</summary>
    public static string CurrentToolVersion { get; } =
        typeof(ScanProvenance).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            // Strip any source-revision suffix the SDK appends ("1.0.0+abc123") — the commit that
            // built the tool is not the commit that was scanned, and showing it here invites the
            // reader to confuse the two.
            ?.Split('+')[0]
        ?? typeof(ScanProvenance).Assembly.GetName().Version?.ToString()
        ?? "unknown";
}
