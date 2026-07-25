namespace MigrationScan.Reporting;

/// <summary>
/// What a report does and does not contain, in the terms a security reviewer needs.
/// </summary>
/// <remarks>
/// MigrationScan's audience is finance, healthcare, defence and government — teams who cannot
/// send source code to a cloud service, and who need a sign-off before emailing anything out.
/// Without this, that sign-off means somebody reading several thousand lines of JSON. The claims
/// below are load-bearing: if a rule ever starts emitting source text, a configuration value, or
/// an absolute path MigrationScan itself derived, this notice is wrong and must change with it.
/// </remarks>
public static class PrivacyNotice
{
    /// <summary>Single-sentence version, for the end of a console run.</summary>
    public const string Summary =
        "This report lists file paths, line numbers and dependency names — never source code, "
        + "file contents, configuration values or credentials.";

    /// <summary>What is in the file.</summary>
    public static readonly IReadOnlyList<string> Includes =
    [
        "Project and source file paths, relative to the folder you scanned.",
        "Line numbers of the code that matched a rule.",
        "Rule identifiers, titles and their fixed remediation text (the same for every scan).",
        "Names and versions of dependencies your projects declare: NuGet packages, referenced "
            + "assemblies, COM components, web-service endpoints, and Windows system libraries "
            + "called via P/Invoke.",
        "Project names as they appear in your solution and project files.",
    ];

    /// <summary>What is not in the file.</summary>
    public static readonly IReadOnlyList<string> Excludes =
    [
        "Any source code, or any part of the contents of any file.",
        "Connection strings, credentials, secrets, or configuration values.",
        "Customer, business or personal data of any kind.",
        "Machine names, user names, or environment details.",
        "Anything from outside the folder you scanned.",
    ];

    /// <summary>
    /// The one caveat worth stating plainly rather than burying: a dependency's declared location
    /// is reproduced exactly as the project file writes it. Almost always relative, but a project
    /// that hard-codes an absolute <c>HintPath</c> will have that path appear verbatim.
    /// </summary>
    public const string Caveat =
        "One exception worth checking: a dependency's `source` is copied exactly as your project "
        + "file declares it. That is normally a relative path, but a project with a hard-coded "
        + "absolute HintPath (for example C:\\Users\\...) will reproduce it as written.";
}
