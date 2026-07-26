namespace MigrationScan.Core.Models;

/// <summary>
/// A non-fatal problem encountered during a scan — for example a project referenced by
/// the solution that is missing or unparseable on disk. Warnings let the scan continue
/// over the rest of the solution instead of failing wholesale, while still surfacing what
/// was skipped (a common situation in large legacy solutions with stale references).
/// </summary>
/// <param name="Message">Human-readable description of what went wrong and what was skipped.</param>
/// <param name="Path">Repo-relative path the warning concerns, or null.</param>
public sealed record ScanWarning(string Message, string? Path)
{
    /// <summary>
    /// Every repo-relative path the <see cref="Message"/> spells out, so a redacted report can
    /// substitute each one instead of guessing at prose.
    /// </summary>
    /// <remarks>
    /// A warning that names several projects in one sentence carries no single
    /// <see cref="Path"/> to swap out, and redaction that cannot clean a warning has to withhold
    /// it. Declaring the paths here is what lets the warning survive redaction with its meaning
    /// intact, which matters because warnings are how a reader learns their coverage was
    /// incomplete.
    /// </remarks>
    public IReadOnlyList<string> MentionedPaths { get; init; } = [];
}
