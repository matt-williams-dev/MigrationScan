namespace MigrationScan.Core.Models;

/// <summary>
/// What kind of dependency a <see cref="ReferenceRecord"/> describes. The kind is read from
/// the declaration form in the project file, so it is always Tier 1 (certain).
/// </summary>
public enum ReferenceKind
{
    /// <summary>A NuGet package, from <c>packages.config</c> or a <c>&lt;PackageReference&gt;</c>.</summary>
    Package,

    /// <summary>
    /// A <c>&lt;Reference&gt;</c> that resolves from the framework or the GAC — no <c>&lt;HintPath&gt;</c>.
    /// </summary>
    Assembly,

    /// <summary>
    /// A <c>&lt;Reference&gt;</c> with a <c>&lt;HintPath&gt;</c> to a checked-in assembly that is not
    /// restored from a package. These are the ones with no upstream to research.
    /// </summary>
    VendoredAssembly,

    /// <summary>
    /// COM or ActiveX: a <c>&lt;COMReference&gt;</c>, a <c>&lt;COMFileReference&gt;</c>, or a generated
    /// interop wrapper (<c>Interop.*</c> / <c>AxInterop.*</c>, or a reference with
    /// <c>&lt;EmbedInteropTypes&gt;</c>).
    /// </summary>
    Com,

    /// <summary>A <c>&lt;ProjectReference&gt;</c> to another project in the same tree.</summary>
    Project,

    /// <summary>
    /// A generated service proxy: a legacy ASMX web reference (<c>&lt;WebReferenceUrl&gt;</c>) or a
    /// WCF service reference (<c>.svcmap</c>).
    /// </summary>
    WebService,
}
