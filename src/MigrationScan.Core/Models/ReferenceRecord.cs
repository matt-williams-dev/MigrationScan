namespace MigrationScan.Core.Models;

/// <summary>
/// One declared dependency of one project — a catalog entry, not a finding. The inventory
/// exists so the third-party surface of a solution can be researched (is this vendor still
/// in business? does this control ship a .NET 10 build?) independently of whether any rule
/// happened to fire on it. Inventory entries carry no severity and no effort, and are not
/// part of the counts, the estimate, or <c>--fail-on</c>.
/// </summary>
/// <param name="Kind">How the dependency was declared.</param>
/// <param name="Name">
/// Package ID, assembly simple name, COM type-library name, referenced project name, or
/// service-reference name.
/// </param>
/// <param name="Version">
/// Package version, assembly version from a strong name, or COM <c>major.minor</c>. Null when
/// the declaration carries no version.
/// </param>
/// <param name="Source">
/// Where the reference points, when the declaration says: a <c>&lt;HintPath&gt;</c>, a referenced
/// project path, a COM type-library GUID, or a service endpoint URL. Null otherwise.
/// </param>
/// <param name="IsFrameworkAssembly">
/// True for a <c>&lt;Reference&gt;</c> to an assembly shipped with .NET Framework itself
/// (<c>System.*</c>, <c>mscorlib</c>, WPF, …). Only ever true for <see cref="ReferenceKind.Assembly"/>:
/// a package named <c>System.Something</c> is still a package you depend on.
/// </param>
/// <param name="ProjectPath">Repo-relative path of the project that declares it.</param>
/// <param name="DeclaredIn">
/// Repo-relative path of the file the declaration was read from — the project file, or
/// <c>packages.config</c> for a legacy package.
/// </param>
/// <param name="Line">1-based line of the declaring element, or null.</param>
public sealed record ReferenceRecord(
    ReferenceKind Kind,
    string Name,
    string? Version,
    string? Source,
    bool IsFrameworkAssembly,
    string ProjectPath,
    string DeclaredIn,
    int? Line)
{
    /// <summary>
    /// External to both the framework and this solution — the set worth researching. Excludes
    /// framework assemblies (they migrate with the runtime) and project references (they are
    /// this solution's own code, already in scope).
    /// </summary>
    public bool IsThirdParty => !IsFrameworkAssembly && Kind is not ReferenceKind.Project;
}
