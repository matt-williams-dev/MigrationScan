namespace MigrationScan.Reporting;

/// <summary>
/// What a report does and does not contain, in the terms a security reviewer needs.
/// </summary>
/// <remarks>
/// MigrationScan's audience is finance, healthcare, defence and government: teams who cannot
/// send source code to a cloud service, and who need a sign-off before emailing anything out.
/// Without this, that sign-off means somebody reading several thousand lines of JSON. The claims
/// below are load-bearing: if a rule ever starts emitting source text, a configuration value, or
/// an absolute path MigrationScan itself derived, this notice is wrong and must change with it.
/// </remarks>
public static class PrivacyNotice
{
    /// <summary>
    /// What the file contains, short enough to sit on the line announcing it was written.
    /// </summary>
    /// <remarks>
    /// A phrase rather than a sentence, because the console already states this once and saying
    /// it twice trains people to skip both. The full account lives in <see cref="Includes"/> and
    /// <see cref="Excludes"/>, which every Markdown report carries. Note what it does not claim:
    /// the report keeps each project's path deliberately, so "no file paths" would be wrong.
    /// </remarks>
    public const string Summary = "no source code · source file paths as opaque ids";

    /// <summary>The same phrase when <c>--include-paths</c> turned redaction off.</summary>
    public const string SummaryWithPaths = "no source code · real file paths, kept by --include-paths";

    /// <summary>What is in the file.</summary>
    public static readonly IReadOnlyList<string> Includes =
    [
        "Project paths, as they appear in your solution: the repo-relative location of each "
            + ".csproj or .vbproj. A project keeps its path where a source file does not, because "
            + "the path is how a project is identified, and findings grouped by project are what "
            + "make the report readable to somebody scoping the work.",
        "Line numbers of the code that matched a rule.",
        "Rule identifiers, titles and their remediation text. These read the same in every scan.",
        "Names and versions of the dependencies your projects declare: NuGet packages, referenced "
            + "assemblies, COM components, web-service endpoints, and the Windows system libraries "
            + "you call through P/Invoke. A name identifies a component; it does not say where it "
            + "sits on disk. We keep names because nobody can assess a component without knowing "
            + "which one it is.",
    ];

    /// <summary>What is not in the file.</summary>
    public static readonly IReadOnlyList<string> Excludes =
    [
        "Source file paths. Each becomes a stable opaque id, so you can still see that two "
            + "findings share a file without the report naming that file.",
        "Source code, and any part of the contents of any file.",
        "Connection strings, credentials, secrets and configuration values.",
        "Web-service hosts and URLs. Only the scheme survives.",
        "Customer, business or personal data of any kind.",
        "Machine names, user names and environment details.",
        "Anything outside the folder you scanned.",
    ];

    /// <summary>
    /// Where redaction applies, and where it does not. Stated plainly because a reader who has
    /// just been told "no paths" will otherwise be surprised by their own console.
    /// </summary>
    public const string Caveat =
        "Redaction covers the JSON report, which is the file you send on. Your console output, "
        + "this Markdown report and the SARIF output all keep full paths. They stay on your "
        + "machine, SARIF exists to point at a line in a file, and hiding paths from your own "
        + "developers would protect nobody. Add --include-paths if you want the JSON to keep "
        + "them too.";
}
