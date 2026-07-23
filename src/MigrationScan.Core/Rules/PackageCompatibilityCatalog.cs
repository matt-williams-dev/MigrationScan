using System.Reflection;
using System.Text.Json;

namespace MigrationScan.Core.Rules;

/// <summary>
/// The bundled list of packages known to have no version supporting modern .NET,
/// used by MIG2001 in the offline path (spec §14: package compatibility lists as data).
/// </summary>
public sealed class PackageCompatibilityCatalog
{
    private const string ResourceSuffix = "Data.incompatible-packages.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IReadOnlyDictionary<string, IncompatiblePackage> _byId;

    private PackageCompatibilityCatalog(IReadOnlyDictionary<string, IncompatiblePackage> byId) => _byId = byId;

    /// <summary>Loads the catalog embedded in this assembly.</summary>
    public static PackageCompatibilityCatalog LoadDefault()
    {
        Assembly assembly = typeof(PackageCompatibilityCatalog).Assembly;
        string resourceName = assembly.GetManifestResourceNames()
            .SingleOrDefault(n => n.EndsWith(ResourceSuffix, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Embedded package catalog '{ResourceSuffix}' was not found.");

        using Stream stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not open embedded package catalog '{resourceName}'.");

        CatalogFile file = JsonSerializer.Deserialize<CatalogFile>(stream, SerializerOptions)
            ?? throw new InvalidOperationException("Package catalog deserialized to null.");

        Dictionary<string, IncompatiblePackage> byId = new(StringComparer.OrdinalIgnoreCase);
        foreach (IncompatiblePackage package in file.Packages)
        {
            byId[package.Id] = package;
        }

        return new PackageCompatibilityCatalog(byId);
    }

    /// <summary>Returns the incompatibility entry for a package ID, or null if it is not listed.</summary>
    public IncompatiblePackage? Find(string packageId) =>
        _byId.TryGetValue(packageId, out IncompatiblePackage? package) ? package : null;

    private sealed record CatalogFile(IReadOnlyList<IncompatiblePackage> Packages);
}

/// <summary>A package known to lack modern-.NET support, with guidance.</summary>
public sealed record IncompatiblePackage(string Id, string Reason, string Replacement);
