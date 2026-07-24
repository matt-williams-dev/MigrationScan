namespace MigrationScan.Core.Models;

/// <summary>
/// A project in the solution that MigrationScan could not analyze — a non-C#/VB project such
/// as a SQL, deployment, or F# project. It is not a finding (there's nothing to remediate in
/// the C# sense), but it is a real scoping input: its migration must be planned separately, so
/// it is surfaced explicitly rather than assumed covered.
/// </summary>
/// <param name="Name">The project's display name in the solution.</param>
/// <param name="Path">Repo-relative, forward-slashed path to the project.</param>
/// <param name="ProjectType">A human-readable project-type label (e.g. "SQL Server database project").</param>
/// <param name="Reason">Why it wasn't assessed.</param>
public sealed record NotAssessedProject(string Name, string Path, string ProjectType, string Reason);
