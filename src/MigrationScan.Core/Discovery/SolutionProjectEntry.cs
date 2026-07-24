namespace MigrationScan.Core.Discovery;

/// <summary>
/// A project referenced by a solution, as parsed from the <c>.sln</c> — not necessarily a
/// C#/VB project (a solution can also contain SSRS/SSIS/setup/database/website projects).
/// </summary>
/// <param name="Name">The project's display name in the solution.</param>
/// <param name="AbsolutePath">Absolute path to the project file (or folder, for web sites).</param>
/// <param name="TypeGuid">The solution project-type GUID (no braces), identifying the tooling.</param>
public sealed record SolutionProjectEntry(string Name, string AbsolutePath, string TypeGuid)
{
    /// <summary>File extension of the project path, lower-cased (e.g. <c>.csproj</c>, <c>.rptproj</c>).</summary>
    public string Extension => System.IO.Path.GetExtension(AbsolutePath).ToLowerInvariant();

    /// <summary>True if this is a C# or VB project MigrationScan can analyze.</summary>
    public bool IsAnalyzable => Extension is ".csproj" or ".vbproj";
}
