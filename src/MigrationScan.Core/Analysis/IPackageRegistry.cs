namespace MigrationScan.Core.Analysis;

/// <summary>
/// Supplies package status looked up from an external source (spec §3, <c>--online</c>).
/// Implementations are synchronous so rules stay simple; the online implementation performs
/// its network I/O behind this interface and caches results. The default is offline and
/// empty, so nothing here runs unless <c>--online</c> is passed.
/// </summary>
public interface IPackageRegistry
{
    /// <summary>
    /// Returns known status for a package, or null if unknown (not found, or offline).
    /// </summary>
    PackageInfo? Lookup(string packageId);
}

/// <summary>Status for a package, as reported by an external registry (e.g. nuget.org).</summary>
/// <param name="Id">The package ID.</param>
/// <param name="IsDeprecated">True if the package is marked deprecated.</param>
/// <param name="DeprecationMessage">The registry's deprecation message, if any.</param>
/// <param name="AlternatePackage">A suggested replacement package ID, if the registry names one.</param>
public sealed record PackageInfo(
    string Id,
    bool IsDeprecated,
    string? DeprecationMessage,
    string? AlternatePackage);

/// <summary>
/// The offline default: knows nothing, reaches no network. Used unless <c>--online</c> is set,
/// keeping the default path deterministic and network-free.
/// </summary>
public sealed class EmptyPackageRegistry : IPackageRegistry
{
    public static EmptyPackageRegistry Instance { get; } = new();

    public PackageInfo? Lookup(string packageId) => null;
}
