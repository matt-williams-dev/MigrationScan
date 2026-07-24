using MigrationScan.Core.Discovery;

namespace MigrationScan.Core.Analysis;

/// <summary>
/// Resolves a scan target (a <c>.sln</c>, a <c>.csproj</c>/<c>.vbproj</c>, or a directory)
/// into the set of project files to analyze, any non-C#/VB projects the solution also
/// contains, and the root directory that output paths are reported relative to.
/// </summary>
/// <param name="RootDirectory">Absolute directory that output paths are relative to.</param>
/// <param name="ProjectFiles">Absolute paths to the C#/VB projects to analyze, ordered.</param>
/// <param name="OtherProjects">Non-C#/VB projects the solution references (empty otherwise).</param>
public sealed record ScanInput(
    string RootDirectory,
    IReadOnlyList<string> ProjectFiles,
    IReadOnlyList<SolutionProjectEntry> OtherProjects)
{
    private static readonly string[] ProjectExtensions = [".csproj", ".vbproj"];

    /// <summary>
    /// Resolves <paramref name="path"/>. Supports a solution file, a single project file,
    /// or a directory scanned recursively for projects.
    /// </summary>
    public static ScanInput Resolve(string path)
    {
        string fullPath = Path.GetFullPath(path);

        if (Directory.Exists(fullPath))
        {
            IReadOnlyList<string> projects = ProjectExtensions
                .SelectMany(ext => Directory.EnumerateFiles(fullPath, $"*{ext}", SearchOption.AllDirectories))
                .ToList();
            return new ScanInput(fullPath, Order(projects, fullPath), []);
        }

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Scan target not found: {path}", fullPath);
        }

        string extension = Path.GetExtension(fullPath);

        if (extension.Equals(".sln", StringComparison.OrdinalIgnoreCase))
        {
            string root = Path.GetDirectoryName(fullPath)!;
            IReadOnlyList<SolutionProjectEntry> entries = SolutionParser.GetProjects(fullPath);
            IReadOnlyList<string> analyzable = entries.Where(e => e.IsAnalyzable).Select(e => e.AbsolutePath).ToList();
            IReadOnlyList<SolutionProjectEntry> others = entries.Where(e => !e.IsAnalyzable).ToList();
            return new ScanInput(root, Order(analyzable, root), others);
        }

        if (IsProjectFile(fullPath))
        {
            return new ScanInput(Path.GetDirectoryName(fullPath)!, [fullPath], []);
        }

        throw new ArgumentException(
            $"Unsupported scan target '{path}'. Expected a .sln, a .csproj/.vbproj, or a directory.",
            nameof(path));
    }

    private static bool IsProjectFile(string filePath) =>
        ProjectExtensions.Contains(Path.GetExtension(filePath), StringComparer.OrdinalIgnoreCase);

    // Deterministic order: by repo-relative path, ordinal. Same input => same output.
    private static IReadOnlyList<string> Order(IEnumerable<string> projectFiles, string rootDirectory) =>
        projectFiles
            .OrderBy(p => PathUtilities.ToRelative(rootDirectory, p), StringComparer.Ordinal)
            .ToList();
}
