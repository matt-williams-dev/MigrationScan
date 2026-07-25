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
    /// <summary>Single-sentence version, for the end of a default console run.</summary>
    public const string Summary =
        "The report contains no file paths and no source code — file locations are replaced with "
        + "opaque ids, so it can be shared without a review.";

    /// <summary>The same line when <c>--include-paths</c> turned redaction off.</summary>
    public const string SummaryWithPaths =
        "This report has --include-paths set, so it contains real file paths. It still contains "
        + "no source code, file contents, configuration values or credentials.";

    /// <summary>What is in the file.</summary>
    public static readonly IReadOnlyList<string> Includes =
    [
        "Project names, as they appear in your solution and project files.",
        "Line numbers of the code that matched a rule.",
        "Rule identifiers, titles and their fixed remediation text (the same for every scan).",
        "Names and versions of dependencies your projects declare: NuGet packages, referenced "
            + "assemblies, COM components, web-service endpoints, and Windows system libraries "
            + "called via P/Invoke. These are identities, not locations, and they are kept "
            + "deliberately — a component cannot be assessed without knowing which one it is.",
    ];

    /// <summary>What is not in the file.</summary>
    public static readonly IReadOnlyList<string> Excludes =
    [
        "Source file paths. Each is replaced by a stable opaque id, so two findings in the same "
            + "file are still visibly in the same file, without naming it.",
        "Any source code, or any part of the contents of any file.",
        "Connection strings, credentials, secrets, or configuration values.",
        "Web-service endpoint hosts and URLs — only the scheme is kept.",
        "Customer, business or personal data of any kind.",
        "Machine names, user names, or environment details.",
        "Anything from outside the folder you scanned.",
    ];

    /// <summary>
    /// Where redaction applies, and where it deliberately does not. Stated plainly because a
    /// reader who has just been told "no paths" will otherwise be surprised by their own console.
    /// </summary>
    public const string Caveat =
        "Redaction applies to the JSON report — the file you share. The console output, this "
        + "Markdown report and the SARIF output keep full paths on purpose: they stay on your "
        + "machine, SARIF exists to annotate a specific line in a specific file, and withholding "
        + "paths from your own developers would help nobody. Run with --include-paths if you want "
        + "the JSON to keep them too.";
}
