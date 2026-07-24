using System.CommandLine;
using MigrationScan.Core.Analysis;
using MigrationScan.Core.Models;
using MigrationScan.Reporting;
using MigrationScan.Tool;

// Exit codes (spec §8):
//   0  no findings above threshold
//   1  findings above --fail-on threshold   (wired in Phase 5)
//   2  analysis error
//  64  bad usage
const int ExitSuccess = 0;
const int ExitFindingsAboveThreshold = 1;
const int ExitAnalysisError = 2;
const int ExitBadUsage = 64;

var pathArgument = new Argument<string>("path")
{
    Description = "A .sln, .csproj, or directory to scan recursively.",
};

var targetOption = new Option<string>("--target")
{
    Description = "Target framework to assess against.",
    DefaultValueFactory = _ => "net10.0",
};

var formatOption = new Option<string[]>("--format", "-f")
{
    Description = "Output format(s): console | json | markdown | sarif. Repeatable.",
    DefaultValueFactory = _ => ["console"],
    AllowMultipleArgumentsPerToken = true,
};

var outputOption = new Option<string?>("--output", "-o")
{
    Description = "Write file-based formats (json, markdown, sarif) here instead of stdout. A directory "
        + "receives report.<ext>; with multiple formats a file path is disambiguated by extension.",
};

var failOnOption = new Option<string?>("--fail-on")
{
    Description = "Exit with code 1 if any finding is at least this severe: blocker | high | medium | low.",
};

var baselineOption = new Option<string?>("--baseline")
{
    Description = "Suppress findings present in a baseline (a JSON report captured earlier).",
};

var rootCommand = new RootCommand(
    "MigrationScan — a free, deterministic, offline .NET Framework migration assessment tool.")
{
    pathArgument,
    targetOption,
    formatOption,
    outputOption,
    failOnOption,
    baselineOption,
};

rootCommand.SetAction(parseResult =>
{
    string path = parseResult.GetValue(pathArgument)!;
    string target = parseResult.GetValue(targetOption)!;
    string[] formats = parseResult.GetValue(formatOption) ?? ["console"];
    string? output = parseResult.GetValue(outputOption);
    string? failOnValue = parseResult.GetValue(failOnOption);
    string? baselinePath = parseResult.GetValue(baselineOption);

    string[] normalizedFormats = formats.Select(f => f.ToLowerInvariant()).Distinct().ToArray();
    string[] unknownFormats = normalizedFormats.Where(f => f is not ("console" or "json" or "markdown" or "sarif")).ToArray();
    if (unknownFormats.Length > 0)
    {
        Console.Error.WriteLine($"Unknown format(s): {string.Join(", ", unknownFormats)}. Expected: console | json | markdown | sarif.");
        return ExitBadUsage;
    }

    Severity? failOn = null;
    if (failOnValue is not null)
    {
        if (!TryParseSeverity(failOnValue, out Severity parsed))
        {
            Console.Error.WriteLine($"Invalid --fail-on value '{failOnValue}'. Expected: blocker | high | medium | low.");
            return ExitBadUsage;
        }

        failOn = parsed;
    }

    if (!File.Exists(path) && !Directory.Exists(path))
    {
        Console.Error.WriteLine($"Scan target not found: {path}");
        return ExitBadUsage;
    }

    if (baselinePath is not null && !File.Exists(baselinePath))
    {
        Console.Error.WriteLine($"Baseline file not found: {baselinePath}");
        return ExitBadUsage;
    }

    try
    {
        SolutionAnalyzer analyzer = SolutionAnalyzer.CreateDefault();
        AnalysisResult result = analyzer.Analyze(path, target);

        if (baselinePath is not null)
        {
            result = ApplyBaseline(result, baselinePath);
        }

        Emit(result, normalizedFormats, output);

        return failOn is { } threshold && result.FailsThreshold(threshold)
            ? ExitFindingsAboveThreshold
            : ExitSuccess;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Analysis error: {ex.Message}");
        return ExitAnalysisError;
    }
});

return rootCommand.Parse(args).Invoke();

static void Emit(AnalysisResult result, string[] formats, string? outputPath)
{
    if (formats.Contains("console"))
    {
        Console.Out.Write(ConsoleReporter.Render(result));
    }

    // File-producing formats: emitted to a file when --output is set, otherwise to stdout.
    (string Name, string Extension, Func<string> Render)[] fileFormats =
    [
        ("json", "json", () => JsonReportWriter.Write(result)),
        ("markdown", "md", () => MarkdownReportWriter.Write(result)),
        ("sarif", "sarif", () => SarifReportWriter.Write(result)),
    ];

    string[] requested = fileFormats.Select(f => f.Name).Where(formats.Contains).ToArray();

    foreach ((string name, string extension, Func<string> render) in fileFormats)
    {
        if (!formats.Contains(name))
        {
            continue;
        }

        string content = render();
        string? destination = FileDestination(outputPath, extension, requested.Length);
        if (destination is null)
        {
            Console.Out.WriteLine(content);
        }
        else
        {
            File.WriteAllText(destination, content);
            Console.Out.WriteLine($"Wrote {name} report to {destination}");
        }
    }
}

// Resolves where a file-format's output goes. null means stdout.
static string? FileDestination(string? outputPath, string extension, int fileFormatCount)
{
    if (outputPath is null)
    {
        return null;
    }

    if (Directory.Exists(outputPath))
    {
        return Path.Combine(outputPath, $"report.{extension}");
    }

    // A single format writes to the given path as-is; multiple formats sharing one path
    // are disambiguated by extension so they don't overwrite each other.
    if (fileFormatCount == 1)
    {
        return outputPath;
    }

    string directory = Path.GetDirectoryName(outputPath) is { Length: > 0 } dir ? dir : ".";
    return Path.Combine(directory, $"{Path.GetFileNameWithoutExtension(outputPath)}.{extension}");
}

static bool TryParseSeverity(string value, out Severity severity) =>
    Enum.TryParse(value, ignoreCase: true, out severity) && Enum.IsDefined(severity);

// Drops findings whose fingerprint is recorded in the baseline report.
static AnalysisResult ApplyBaseline(AnalysisResult result, string baselinePath)
{
    IReadOnlySet<string> baseline = BaselineReader.LoadFingerprints(baselinePath);
    List<Finding> kept = result.Findings.Where(f => !baseline.Contains(Fingerprints.Of(f))).ToList();

    int suppressed = result.Findings.Count - kept.Count;
    if (suppressed > 0)
    {
        Console.Error.WriteLine($"Suppressed {suppressed} finding(s) present in the baseline.");
    }

    return result with { Findings = kept };
}
