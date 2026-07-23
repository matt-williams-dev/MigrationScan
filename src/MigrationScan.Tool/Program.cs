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
    Description = "Output format(s): console | json. Repeatable.",
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
    string[] unknownFormats = normalizedFormats.Where(f => f is not ("console" or "json")).ToArray();
    if (unknownFormats.Length > 0)
    {
        Console.Error.WriteLine($"Unknown format(s): {string.Join(", ", unknownFormats)}. Expected: console | json.");
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

    if (formats.Contains("json"))
    {
        string json = JsonReportWriter.Write(result);
        if (outputPath is not null)
        {
            File.WriteAllText(outputPath, json);
            Console.Out.WriteLine($"Wrote JSON report to {outputPath}");
        }
        else
        {
            Console.Out.WriteLine(json);
        }
    }
}
