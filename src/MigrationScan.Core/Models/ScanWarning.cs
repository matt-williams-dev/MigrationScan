namespace MigrationScan.Core.Models;

/// <summary>
/// A non-fatal problem encountered during a scan — for example a project referenced by
/// the solution that is missing or unparseable on disk. Warnings let the scan continue
/// over the rest of the solution instead of failing wholesale, while still surfacing what
/// was skipped (a common situation in large legacy solutions with stale references).
/// </summary>
/// <param name="Message">Human-readable description of what went wrong and what was skipped.</param>
/// <param name="Path">Repo-relative path the warning concerns, or null.</param>
public sealed record ScanWarning(string Message, string? Path);
