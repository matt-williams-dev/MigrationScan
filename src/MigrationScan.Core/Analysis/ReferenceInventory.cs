using System.Xml;
using System.Xml.Linq;
using MigrationScan.Core.Discovery;
using MigrationScan.Core.Engine;
using MigrationScan.Core.Models;

namespace MigrationScan.Core.Analysis;

/// <summary>
/// Catalogs everything a project declares a dependency on — packages, assemblies, COM and
/// ActiveX interop, sibling projects, and generated service proxies. This is inventory, not
/// findings: it answers "what does this solution depend on, and where does each piece come
/// from" so the third-party surface can be researched before a migration is scoped.
///
/// Everything here is read from XML (spec §3: no MSBuild evaluation), so every entry is
/// Tier 1. Nothing is resolved on disk — a reference to a missing file is still recorded,
/// because a dangling dependency is exactly the kind of thing a scoping exercise needs to see.
/// </summary>
public static class ReferenceInventory
{
    /// <summary>Collects every declared reference of the project in <paramref name="context"/>.</summary>
    public static IReadOnlyList<ReferenceRecord> Collect(AnalysisContext context)
    {
        List<ReferenceRecord> packages = Packages(context).ToList();

        return
        [
            .. packages,
            .. Assemblies(context, packages),
            .. ComReferences(context),
            .. ProjectReferences(context),
            .. ServiceReferences(context),
        ];
    }

    /// <summary>Deterministic order: by project, then kind, then name, then declaration site.</summary>
    public static IReadOnlyList<ReferenceRecord> Sort(IEnumerable<ReferenceRecord> references) =>
        references
            .Distinct()
            .OrderBy(r => r.ProjectPath, StringComparer.Ordinal)
            .ThenBy(r => r.Kind)
            .ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => r.Version, StringComparer.Ordinal)
            .ThenBy(r => r.DeclaredIn, StringComparer.Ordinal)
            .ThenBy(r => r.Line ?? 0)
            .ToList();

    // NuGet packages, from packages.config and <PackageReference> alike. A package is never
    // classified as a framework assembly: "System.Text.Json" as a package is a real dependency
    // with its own release cadence, unlike the System.Text.Json that ships in the runtime.
    private static IEnumerable<ReferenceRecord> Packages(AnalysisContext context) =>
        context.Packages.Select(package => new ReferenceRecord(
            ReferenceKind.Package,
            package.Id,
            package.Version,
            Source: null,
            IsFrameworkAssembly: false,
            context.ProjectRelativePath,
            package.DeclaredIn,
            package.Line));

    // <Reference> elements. Three outcomes: a COM interop wrapper, a vendored DLL, or an
    // assembly resolved from the framework/GAC.
    private static IEnumerable<ReferenceRecord> Assemblies(
        AnalysisContext context, IReadOnlyList<ReferenceRecord> packagesSoFar)
    {
        HashSet<string> knownPackages = packagesSoFar
            .Where(r => r.Kind == ReferenceKind.Package)
            .Select(r => r.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (AssemblyReferenceInfo reference in context.AssemblyReferences)
        {
            // When a package of the same name is already declared, that entry is the
            // authoritative record of the dependency — it carries the package version (13.0.3)
            // rather than the assembly version (13.0.0.0) — and this element is just the binding
            // detail. Legacy projects declare both routinely, sometimes inconsistently (a
            // packages.config entry alongside a GAC-resolving <Reference>). Two rows would read
            // as two things to research when there is one; where the assembly actually resolves
            // from is reported as a finding (MIG1005/MIG1010), which is the right place for it.
            if (knownPackages.Contains(reference.SimpleName))
            {
                continue;
            }

            bool isRestoredPackage = reference.HintPath is { } path && IsPackagePath(path);

            yield return new ReferenceRecord(
                ClassifyAssembly(reference, isRestoredPackage),
                reference.SimpleName,
                reference.Version,
                // Hint paths are written with Windows separators; normalize so output reads the
                // same on every host (spec §3).
                reference.HintPath is { } hint ? PathUtilities.NormalizeSeparators(hint) : null,
                // Only a genuine framework assembly reference — a vendored or restored DLL
                // named System.Something is still somebody's shipped binary.
                IsFrameworkAssembly: reference.HintPath is null && FrameworkAssemblies.Contains(reference.SimpleName),
                context.ProjectRelativePath,
                context.ProjectRelativePath,
                reference.Line);
        }
    }

    private static ReferenceKind ClassifyAssembly(AssemblyReferenceInfo reference, bool isRestoredPackage)
    {
        // Generated COM/ActiveX wrappers. tlbimp emits "Interop.Foo"; aximp emits "AxInterop.Foo"
        // for the ActiveX host control. EmbedInteropTypes marks an embedded PIA either way.
        if (reference.EmbedInteropTypes
            || reference.SimpleName.StartsWith("Interop.", StringComparison.OrdinalIgnoreCase)
            || reference.SimpleName.StartsWith("AxInterop.", StringComparison.OrdinalIgnoreCase))
        {
            return ReferenceKind.Com;
        }

        if (isRestoredPackage)
        {
            return ReferenceKind.Package;
        }

        return reference.HintPath is null ? ReferenceKind.Assembly : ReferenceKind.VendoredAssembly;
    }

    // A HintPath under a packages folder or the NuGet cache is a package restore, not a
    // checked-in binary. Mirrors the test MIG1010 applies.
    private static bool IsPackagePath(string hintPath)
    {
        string normalized = hintPath.Replace('\\', '/');
        return normalized.Contains("/packages/", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("packages/", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains(".nuget", StringComparison.OrdinalIgnoreCase);
    }

    // <COMReference> (type library, imported by tlbimp) and <COMFileReference> (a .tlb/.dll
    // on disk). The type-library GUID is the identity worth researching, so it becomes Source.
    private static IEnumerable<ReferenceRecord> ComReferences(AnalysisContext context)
    {
        foreach (XElement com in context.Document.Descendants(context.Namespace + "COMReference"))
        {
            string? guid = com.Element(context.Namespace + "Guid")?.Value.Trim();
            yield return new ReferenceRecord(
                ReferenceKind.Com,
                com.Attribute("Include")?.Value.Trim() is { Length: > 0 } include ? include : guid ?? "(unnamed)",
                ComVersion(com, context.Namespace),
                string.IsNullOrWhiteSpace(guid) ? null : guid,
                IsFrameworkAssembly: false,
                context.ProjectRelativePath,
                context.ProjectRelativePath,
                LineOf(com));
        }

        foreach (XElement com in context.Document.Descendants(context.Namespace + "COMFileReference"))
        {
            if (com.Attribute("Include")?.Value.Trim() is not { Length: > 0 } include)
            {
                continue;
            }

            yield return new ReferenceRecord(
                ReferenceKind.Com,
                Path.GetFileName(PathUtilities.NormalizeSeparators(include)),
                Version: null,
                PathUtilities.NormalizeSeparators(include),
                IsFrameworkAssembly: false,
                context.ProjectRelativePath,
                context.ProjectRelativePath,
                LineOf(com));
        }
    }

    // COM references carry the type-library version split across two elements.
    private static string? ComVersion(XElement com, XNamespace ns)
    {
        string? major = com.Element(ns + "VersionMajor")?.Value.Trim();
        string? minor = com.Element(ns + "VersionMinor")?.Value.Trim();
        return string.IsNullOrEmpty(major) ? null : $"{major}.{(string.IsNullOrEmpty(minor) ? "0" : minor)}";
    }

    // <ProjectReference> — the solution-internal dependency graph. Recorded relative to the
    // scan root so the same project referenced from two places produces the same entry.
    private static IEnumerable<ReferenceRecord> ProjectReferences(AnalysisContext context)
    {
        foreach (XElement element in context.Document.Descendants(context.Namespace + "ProjectReference"))
        {
            if (element.Attribute("Include")?.Value.Trim() is not { Length: > 0 } include)
            {
                continue;
            }

            string absolute = Path.GetFullPath(
                Path.Combine(context.ProjectDirectory, include.Replace('\\', Path.DirectorySeparatorChar)));

            yield return new ReferenceRecord(
                ReferenceKind.Project,
                Path.GetFileNameWithoutExtension(absolute),
                Version: null,
                context.ToRelative(absolute),
                IsFrameworkAssembly: false,
                context.ProjectRelativePath,
                context.ProjectRelativePath,
                LineOf(element));
        }
    }

    // Generated service proxies: a legacy ASMX web reference, or a WCF service reference.
    // Both point at an endpoint that has to exist (or be replaced) after migration, which is
    // why they belong in the inventory even though the generated code is skipped by the scan.
    private static IEnumerable<ReferenceRecord> ServiceReferences(AnalysisContext context)
    {
        // ASMX: <WebReferenceUrl Include="http://host/Service.asmx"><RelPath>Web References\Foo\</RelPath>
        foreach (XElement element in context.Document.Descendants(context.Namespace + "WebReferenceUrl"))
        {
            if (element.Attribute("Include")?.Value.Trim() is not { Length: > 0 } url)
            {
                continue;
            }

            string? relPath = element.Element(context.Namespace + "RelPath")?.Value;
            yield return new ReferenceRecord(
                ReferenceKind.WebService,
                ServiceName(relPath) ?? url,
                Version: null,
                url,
                IsFrameworkAssembly: false,
                context.ProjectRelativePath,
                context.ProjectRelativePath,
                LineOf(element));
        }

        // WCF: the .svcmap sits under "Service References\<name>\" as an ordinary project item.
        foreach (XElement element in context.Document.Descendants())
        {
            if (element.Attribute("Include")?.Value.Trim() is not { Length: > 0 } include
                || !include.EndsWith(".svcmap", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string normalized = PathUtilities.NormalizeSeparators(include);
            yield return new ReferenceRecord(
                ReferenceKind.WebService,
                ServiceName(Path.GetDirectoryName(normalized)) ?? normalized,
                Version: null,
                SvcMapAddress(context, normalized) ?? normalized,
                IsFrameworkAssembly: false,
                context.ProjectRelativePath,
                context.ProjectRelativePath,
                LineOf(element));
        }
    }

    // "Service References\CustomerService\" -> "CustomerService".
    private static string? ServiceName(string? folder)
    {
        if (string.IsNullOrWhiteSpace(folder))
        {
            return null;
        }

        string name = PathUtilities.NormalizeSeparators(folder).TrimEnd('/').Split('/')[^1];
        return name.Length == 0 ? null : name;
    }

    // The endpoint the proxy was generated from. Best-effort: an unreadable or reshaped
    // .svcmap falls back to the file path rather than failing the scan.
    private static string? SvcMapAddress(AnalysisContext context, string relativePath)
    {
        string absolute = Path.Combine(context.ProjectDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(absolute))
        {
            return null;
        }

        try
        {
            return XDocument.Load(absolute)
                .Descendants()
                .FirstOrDefault(e => e.Name.LocalName == "MetadataSource")
                ?.Attribute("Address")?.Value.Trim() is { Length: > 0 } address
                ? address
                : null;
        }
        catch (Exception ex) when (ex is XmlException or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static int? LineOf(XElement element) =>
        element is IXmlLineInfo info && info.HasLineInfo() ? info.LineNumber : null;
}
