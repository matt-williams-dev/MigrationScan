namespace MigrationScan.Core.Engine;

/// <summary>
/// A NuGet package the project depends on, from either <c>packages.config</c> or a
/// <c>&lt;PackageReference&gt;</c> element.
/// </summary>
/// <param name="Id">Package ID.</param>
/// <param name="Version">Declared version, or null if none was found.</param>
/// <param name="DeclaredIn">Repo-relative path of the file that declared it.</param>
/// <param name="Line">1-based line of the declaring element, or null.</param>
public sealed record PackageReferenceInfo(string Id, string? Version, string DeclaredIn, int? Line);
