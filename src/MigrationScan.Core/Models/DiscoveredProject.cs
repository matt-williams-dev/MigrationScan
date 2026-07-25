namespace MigrationScan.Core.Models;

/// <summary>
/// A project discovered during scanning and parsed as XML (no MSBuild evaluation).
/// </summary>
/// <param name="Path">Repo-relative path to the project file, forward-slashed.</param>
/// <param name="Name">Project name (file name without extension).</param>
/// <param name="IsSdkStyle">True when the root element carries an <c>Sdk</c> attribute.</param>
/// <param name="TargetFramework">
/// Raw target framework moniker or version as written in the project
/// (e.g. <c>net10.0</c> or <c>v4.7.2</c>), or null if none was declared.
/// </param>
/// <param name="RootElementLine">1-based line of the root <c>&lt;Project&gt;</c> element.</param>
/// <remarks>
/// Deliberately carries no reference list: what a project depends on is catalogued in
/// <see cref="AnalysisResult.References"/>, which records kind, version, and origin rather
/// than bare include strings.
/// </remarks>
public sealed record DiscoveredProject(
    string Path,
    string Name,
    bool IsSdkStyle,
    string? TargetFramework,
    int RootElementLine);
