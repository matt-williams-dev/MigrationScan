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
    Description = "Output format(s): console | json | markdown. Repeatable.",
    DefaultValueFactory = _ => ["console"],
    AllowMultipleArgumentsPerToken = true,
};

var outputOption = new Option<string?>("--output", "-o")
{
    Description = "Write file-based formats (e.g. json) to this path instead of stdout.",
};

var rootCommand = new RootCommand(
    "MigrationScan — a free, deterministic, offline .NET Framework migration assessment tool.")
{
    pathArgument,
    targetOption,
    formatOption,
    outputOption,
};

rootCommand.SetAction(parseResult =>
{
    string path = parseResult.GetValue(pathArgument)!;
    string target = parseResult.GetValue(targetOption)!;
    string[] formats = parseResult.GetValue(formatOption) ?? ["console"];
    string? output = parseResult.GetValue(outputOption);

    string[] normalizedFormats = formats.Select(f => f.ToLowerInvariant()).Distinct().ToArray();
    string[] unknownFormats = normalizedFormats.Where(f => f is not ("console" or "json" or "markdown")).ToArray();
    if (unknownFormats.Length > 0)
    {
        Console.Error.WriteLine($"Unknown format(s): {string.Join(", ", unknownFormats)}. Expected: console | json | markdown.");
        return ExitBadUsage;
    }

    if (!File.Exists(path) && !Directory.Exists(path))
    {
        Console.Error.WriteLine($"Scan target not found: {path}");
        return ExitBadUsage;
    }

    try
    {
        SolutionAnalyzer analyzer = SolutionAnalyzer.CreateDefault();
        AnalysisResult result = analyzer.Analyze(path, target);
        Emit(result, normalizedFormats, output);
        return ExitSuccess;
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
