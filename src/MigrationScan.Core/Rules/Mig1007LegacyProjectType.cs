namespace MigrationScan.Core.Rules;

/// <summary>
/// MIG1007 — Legacy project type (Silverlight, Web Site, setup/<c>.vdproj</c>, SSIS, SSRS).
///
/// Unlike the other rules, this is detected at the solution level rather than per project:
/// these project types are not C#/VB and have no <see cref="Engine.AnalysisContext"/>, so the
/// finding is produced from the <c>.sln</c> entry by
/// <see cref="Analysis.SolutionProjectAnalyzer"/>. This class only carries the rule ID.
/// </summary>
public static class Mig1007LegacyProjectType
{
    public const string Id = "MIG1007";
}
