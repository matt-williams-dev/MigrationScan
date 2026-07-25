namespace MigrationScan.Core.Engine;

/// <summary>
/// A <c>&lt;Reference&gt;</c> (assembly reference) declared in a project file.
/// </summary>
/// <param name="Include">The raw <c>Include</c> value (may be a simple or strong name).</param>
/// <param name="SimpleName">The assembly's simple name (the part before the first comma).</param>
/// <param name="Version">The <c>Version=</c> component of a strong name, or null.</param>
/// <param name="HintPath">
/// The <c>&lt;HintPath&gt;</c> value, trimmed, or null when absent or blank. A blank hint path
/// points nowhere, so it is treated as absent — the reference still resolves from the GAC.
/// </param>
/// <param name="EmbedInteropTypes">True when <c>&lt;EmbedInteropTypes&gt;</c> is <c>true</c>.</param>
/// <param name="IsStrongNamed">True when the include carries a <c>PublicKeyToken</c>.</param>
/// <param name="Line">1-based line of the element, or null.</param>
public sealed record AssemblyReferenceInfo(
    string Include,
    string SimpleName,
    string? Version,
    string? HintPath,
    bool EmbedInteropTypes,
    bool IsStrongNamed,
    int? Line)
{
    /// <summary>True when the reference declares a usable <c>&lt;HintPath&gt;</c>.</summary>
    public bool HasHintPath => HintPath is not null;
}
