using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.VisualBasic;
using MigrationScan.Core.Analysis;
using MigrationScan.Core.Discovery;
using MigrationScan.Core.Models;

namespace MigrationScan.Core.Engine;

/// <summary>
/// Everything a rule needs to analyze a single project: the parsed project XML, its
/// package and assembly references, its C# source as Roslyn syntax trees, and helpers
/// for inspecting files on disk. Expensive data (syntax trees, references) is parsed
/// lazily and once, so project-file rules never pay for Roslyn.
/// </summary>
public sealed class AnalysisContext
{
    private readonly string _rootDirectory;
    private readonly Lazy<IReadOnlyList<SourceFile>> _sourceFiles;
    private readonly Lazy<IReadOnlyList<PackageReferenceInfo>> _packages;
    private readonly Lazy<IReadOnlyList<AssemblyReferenceInfo>> _assemblyReferences;

    private AnalysisContext(
        string rootDirectory,
        string projectAbsolutePath,
        string targetFramework,
        XDocument document,
        DiscoveredProject project,
        IPackageRegistry packageRegistry)
    {
        _rootDirectory = rootDirectory;
        ProjectAbsolutePath = projectAbsolutePath;
        ProjectDirectory = Path.GetDirectoryName(projectAbsolutePath)!;
        TargetFramework = targetFramework;
        Document = document;
        Namespace = document.Root!.GetDefaultNamespace();
        Project = project;
        PackageRegistry = packageRegistry;

        _sourceFiles = new Lazy<IReadOnlyList<SourceFile>>(LoadSourceFiles);
        _packages = new Lazy<IReadOnlyList<PackageReferenceInfo>>(LoadPackages);
        _assemblyReferences = new Lazy<IReadOnlyList<AssemblyReferenceInfo>>(LoadAssemblyReferences);
    }

    /// <summary>The project summary (SDK-style flag, target framework, references).</summary>
    public DiscoveredProject Project { get; }

    /// <summary>Absolute path to the project file.</summary>
    public string ProjectAbsolutePath { get; }

    /// <summary>Absolute path to the directory containing the project file.</summary>
    public string ProjectDirectory { get; }

    /// <summary>Repo-relative, forward-slashed path of the project file.</summary>
    public string ProjectRelativePath => Project.Path;

    /// <summary>The target framework the scan is assessing against.</summary>
    public string TargetFramework { get; }

    /// <summary>The parsed project document (loaded with line info).</summary>
    public XDocument Document { get; }

    /// <summary>The project's default XML namespace (empty for SDK-style projects).</summary>
    public XNamespace Namespace { get; }

    /// <summary>C# source files under the project directory, parsed into syntax trees.</summary>
    public IReadOnlyList<SourceFile> SourceFiles => _sourceFiles.Value;

    /// <summary>Packages from <c>packages.config</c> and <c>PackageReference</c>, in file order.</summary>
    public IReadOnlyList<PackageReferenceInfo> Packages => _packages.Value;

    /// <summary>Assembly <c>&lt;Reference&gt;</c> elements declared in the project file.</summary>
    public IReadOnlyList<AssemblyReferenceInfo> AssemblyReferences => _assemblyReferences.Value;

    /// <summary>External package status source (offline/empty unless <c>--online</c>).</summary>
    public IPackageRegistry PackageRegistry { get; }

    /// <summary>Builds a context for one project, loading the project XML once.</summary>
    public static AnalysisContext Create(
        string rootDirectory,
        string projectAbsolutePath,
        string targetFramework,
        IPackageRegistry? packageRegistry = null)
    {
        XDocument document = XDocument.Load(projectAbsolutePath, LoadOptions.SetLineInfo);
        string relativePath = PathUtilities.ToRelative(rootDirectory, projectAbsolutePath);
        DiscoveredProject project = ProjectParser.ParseFrom(document, projectAbsolutePath, relativePath);
        return new AnalysisContext(
            rootDirectory, projectAbsolutePath, targetFramework, document, project,
            packageRegistry ?? EmptyPackageRegistry.Instance);
    }

    /// <summary>True if the project directory contains any file with one of the given extensions.</summary>
    public bool HasFilesWithExtension(params string[] extensions)
    {
        HashSet<string> wanted = new(extensions, StringComparer.OrdinalIgnoreCase);
        return EnumerateProjectFiles()
            .Any(path => wanted.Contains(Path.GetExtension(path)));
    }

    /// <summary>True if a sibling file (e.g. <c>packages.config</c>) exists next to the project.</summary>
    public bool HasSiblingFile(string fileName) => File.Exists(Path.Combine(ProjectDirectory, fileName));

    /// <summary>Repo-relative, forward-slashed form of an absolute path.</summary>
    public string ToRelative(string absolutePath) => PathUtilities.ToRelative(_rootDirectory, absolutePath);

    private IReadOnlyList<SourceFile> LoadSourceFiles() =>
        EnumerateProjectFiles()
            .Where(IsSourceFile)
            .Select(path => new { Relative = ToRelative(path), Path = path })
            .OrderBy(x => x.Relative, StringComparer.Ordinal)
            .Select(x => new SourceFile(x.Relative, ParseSource(x.Path, x.Relative)))
            .ToList();

    private static bool IsSourceFile(string path)
    {
        string extension = Path.GetExtension(path);
        return extension.Equals(".cs", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".vb", StringComparison.OrdinalIgnoreCase);
    }

    private static SyntaxTree ParseSource(string path, string relativePath)
    {
        string text = File.ReadAllText(path);
        return Path.GetExtension(path).Equals(".vb", StringComparison.OrdinalIgnoreCase)
            ? VisualBasicSyntaxTree.ParseText(text, path: relativePath)
            : CSharpSyntaxTree.ParseText(text, path: relativePath);
    }

    private IReadOnlyList<PackageReferenceInfo> LoadPackages()
    {
        List<PackageReferenceInfo> packages = [];

        // Legacy: packages.config next to the project.
        string packagesConfig = Path.Combine(ProjectDirectory, "packages.config");
        if (File.Exists(packagesConfig))
        {
            string relative = ToRelative(packagesConfig);
            XDocument config = XDocument.Load(packagesConfig, LoadOptions.SetLineInfo);
            foreach (XElement package in config.Descendants("package"))
            {
                string? id = package.Attribute("id")?.Value;
                if (!string.IsNullOrWhiteSpace(id))
                {
                    packages.Add(new PackageReferenceInfo(id.Trim(), package.Attribute("version")?.Value, relative, LineOf(package)));
                }
            }
        }

        // SDK-style: <PackageReference Include="..." Version="..." />.
        foreach (XElement reference in Document.Descendants(Namespace + "PackageReference"))
        {
            string? id = reference.Attribute("Include")?.Value;
            if (!string.IsNullOrWhiteSpace(id))
            {
                string? version = reference.Attribute("Version")?.Value
                    ?? reference.Element(Namespace + "Version")?.Value;
                packages.Add(new PackageReferenceInfo(id.Trim(), version, ProjectRelativePath, LineOf(reference)));
            }
        }

        return packages;
    }

    private IReadOnlyList<AssemblyReferenceInfo> LoadAssemblyReferences()
    {
        List<AssemblyReferenceInfo> references = [];

        foreach (XElement reference in Document.Descendants(Namespace + "Reference"))
        {
            string? include = reference.Attribute("Include")?.Value;
            if (string.IsNullOrWhiteSpace(include))
            {
                continue;
            }

            include = include.Trim();
            string simpleName = include.Split(',', 2)[0].Trim();
            // An empty or whitespace <HintPath> does not point anywhere — treat it as absent.
            bool hasHintPath = !string.IsNullOrWhiteSpace(reference.Element(Namespace + "HintPath")?.Value);
            bool isStrongNamed = include.Contains("PublicKeyToken", StringComparison.OrdinalIgnoreCase);

            references.Add(new AssemblyReferenceInfo(include, simpleName, hasHintPath, isStrongNamed, LineOf(reference)));
        }

        return references;
    }

    // Files belonging to this project: everything under its directory, but not descending
    // into build output (bin/obj), hidden folders (.git, .vs, …), node_modules, or a nested
    // project's directory — those files belong to that other project, not this one.
    private IEnumerable<string> EnumerateProjectFiles() => WalkProjectFiles(ProjectDirectory);

    private static IEnumerable<string> WalkProjectFiles(string directory)
    {
        foreach (string file in Directory.EnumerateFiles(directory))
        {
            yield return file;
        }

        foreach (string subdirectory in Directory.EnumerateDirectories(directory))
        {
            string name = Path.GetFileName(subdirectory);
            if (IsExcludedDirectory(name) || ContainsProjectFile(subdirectory))
            {
                continue;
            }

            foreach (string file in WalkProjectFiles(subdirectory))
            {
                yield return file;
            }
        }
    }

    private static bool IsExcludedDirectory(string name) =>
        name.StartsWith('.') // hidden: .git, .vs, .idea, …
        || name.Equals("bin", StringComparison.OrdinalIgnoreCase)
        || name.Equals("obj", StringComparison.OrdinalIgnoreCase)
        || name.Equals("node_modules", StringComparison.OrdinalIgnoreCase);

    // A subdirectory that has its own project file is a separate project; its files are
    // that project's, and are scanned when that project is analyzed — not absorbed here.
    private static bool ContainsProjectFile(string directory) =>
        Directory.EnumerateFiles(directory, "*.csproj").Any()
        || Directory.EnumerateFiles(directory, "*.vbproj").Any();

    private static int? LineOf(XElement element) =>
        element is IXmlLineInfo lineInfo && lineInfo.HasLineInfo() ? lineInfo.LineNumber : null;
}
