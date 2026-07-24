using System.Xml;
using MigrationScan.Core.Discovery;
using MigrationScan.Core.Engine;
using MigrationScan.Core.Models;
using MigrationScan.Core.Rules;

namespace MigrationScan.Core.Analysis;

/// <summary>
/// Runs the analysis: resolve the scan target, build a context per project, apply the
/// rule engine, and return findings in deterministic order.
///
/// A project that is missing or unparseable is skipped with a warning rather than
/// failing the whole scan — large legacy solutions routinely carry stale project
/// references, and one broken project should not abort the assessment.
/// </summary>
public sealed class SolutionAnalyzer
{
    private readonly RuleEngine _engine;
    private readonly IPackageRegistry _packageRegistry;
    private readonly RuleCatalog _catalog;

    public SolutionAnalyzer(RuleEngine engine, IPackageRegistry? packageRegistry = null, RuleCatalog? catalog = null)
    {
        _engine = engine;
        _packageRegistry = packageRegistry ?? EmptyPackageRegistry.Instance;
        _catalog = catalog ?? RuleCatalog.LoadDefault();
    }

    /// <summary>Builds an analyzer over the given rule catalog and the default package catalog.</summary>
    public SolutionAnalyzer(RuleCatalog catalog, IPackageRegistry? packageRegistry = null)
        : this(new RuleEngine(DefaultRules.CreateAll(catalog, PackageCompatibilityCatalog.LoadDefault())), packageRegistry, catalog)
    {
    }

    /// <summary>
    /// Builds an analyzer with the full built-in rule set. Pass a package registry to enable
    /// online lookups (<c>--online</c>); the default is offline and reaches no network.
    /// </summary>
    public static SolutionAnalyzer CreateDefault(IPackageRegistry? packageRegistry = null) =>
        new(RuleCatalog.LoadDefault(), packageRegistry);

    public AnalysisResult Analyze(string path, string targetFramework)
    {
        // A compiled assembly target (no source) is analyzed with Cecil instead (Tier 3).
        string extension = Path.GetExtension(path);
        if (File.Exists(path) && (extension.Equals(".dll", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".exe", StringComparison.OrdinalIgnoreCase)))
        {
            string root = Path.GetDirectoryName(Path.GetFullPath(path))!;
            (DiscoveredProject project, IReadOnlyList<Finding> binaryFindings) = BinaryAnalyzer.Analyze(path, root);
            return new AnalysisResult(targetFramework, [project], Sort(binaryFindings, targetFramework), []);
        }

        ScanInput input = ScanInput.Resolve(path);

        List<DiscoveredProject> projects = [];
        List<Finding> findings = [];
        List<ScanWarning> warnings = [];

        foreach (string projectFile in input.ProjectFiles)
        {
            string relativePath = PathUtilities.ToRelative(input.RootDirectory, projectFile);

            try
            {
                AnalysisContext context = AnalysisContext.Create(
                    input.RootDirectory, projectFile, targetFramework, _packageRegistry);
                IReadOnlyList<Finding> projectFindings = _engine.Analyze(context);
                projects.Add(context.Project);
                findings.AddRange(projectFindings);
            }
            catch (Exception ex) when (IsRecoverable(ex))
            {
                warnings.Add(new ScanWarning($"Skipped '{relativePath}': {Describe(ex)}", relativePath));
            }
        }

        // Non-C#/VB projects the solution also references (SSRS/SSIS/setup -> MIG1007; the rest
        // -> the "not assessed" list, so coverage isn't silently overstated).
        (IReadOnlyList<Finding> otherFindings, IReadOnlyList<NotAssessedProject> notAssessed) =
            SolutionProjectAnalyzer.Analyze(input.OtherProjects, input.RootDirectory, _catalog);
        findings.AddRange(otherFindings);

        return new AnalysisResult(targetFramework, projects, Sort(findings, targetFramework), SortWarnings(warnings))
        {
            NotAssessed = notAssessed.OrderBy(p => p.Path, StringComparer.Ordinal).ToList(),
        };
    }

    // A broken individual project is recoverable — skip it and warn. Anything else
    // (out of memory, access denied at the root, …) is left to propagate as an error.
    private static bool IsRecoverable(Exception ex) =>
        ex is FileNotFoundException
            or DirectoryNotFoundException
            or XmlException
            or InvalidDataException;

    private static string Describe(Exception ex) => ex switch
    {
        FileNotFoundException => "project file not found (referenced by the solution but missing on disk).",
        DirectoryNotFoundException => "project directory not found.",
        XmlException xml => $"malformed project XML ({xml.Message}).",
        InvalidDataException data => data.Message,
        _ => ex.Message,
    };

    // Collapse identical findings (same rule, location, and message) — e.g. a rule that
    // matches two related identifiers on one line — then order stably so the same input
    // always produces byte-identical output. Windows lock-in findings are marked "satisfied"
    // when the target is a Windows TFM, so the reports downgrade rather than count them.
    private static IReadOnlyList<Finding> Sort(IEnumerable<Finding> findings, string targetFramework)
    {
        bool targetIsWindows = TargetPlatform.IsWindows(targetFramework);
        return findings
            .Distinct()
            .OrderBy(f => f.ProjectPath, StringComparer.Ordinal)
            .ThenBy(f => f.Rule.Id, StringComparer.Ordinal)
            .ThenBy(f => f.Line ?? 0)
            .ThenBy(f => f.FilePath, StringComparer.Ordinal)
            .ThenBy(f => f.Message, StringComparer.Ordinal)
            .Select(f => targetIsWindows && f.Rule.Platform == RulePlatform.Windows
                ? f with { SatisfiedByTarget = true }
                : f)
            .ToList();
    }

    private static IReadOnlyList<ScanWarning> SortWarnings(IEnumerable<ScanWarning> warnings) =>
        warnings
            .OrderBy(w => w.Path, StringComparer.Ordinal)
            .ThenBy(w => w.Message, StringComparer.Ordinal)
            .ToList();
}
