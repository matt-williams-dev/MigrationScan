namespace MigrationScan.Core.Engine;

/// <summary>
/// A <c>&lt;Reference&gt;</c> (assembly reference) declared in a project file.
/// </summary>
/// <param name="Include">The raw <c>Include</c> value (may be a simple or strong name).</param>
/// <param name="SimpleName">The assembly's simple name (the part before the first comma).</param>
/// <param name="HasHintPath">True when a <c>&lt;HintPath&gt;</c> child is present.</param>
/// <param name="IsStrongNamed">True when the include carries a <c>PublicKeyToken</c>.</param>
/// <param name="Line">1-based line of the element, or null.</param>
public sealed record AssemblyReferenceInfo(
    string Include,
    string SimpleName,
    bool HasHintPath,
    bool IsStrongNamed,
    int? Line);
