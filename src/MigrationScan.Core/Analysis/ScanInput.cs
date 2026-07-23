using MigrationScan.Core.Discovery;

namespace MigrationScan.Core.Analysis;

/// <summary>
/// Resolves a scan target (a <c>.sln</c>, a <c>.csproj</c>, or a directory) into the
/// set of project files to analyze and the root directory that output paths are
/// reported relative to.
/// </summary>
/// <param name="RootDirectory">Absolute directory that output paths are relative to.</param>
/// <param name="ProjectFiles">Absolute paths to the C# projects to analyze, ordered.</param>
public sealed record ScanInput(string RootDirectory, IReadOnlyList<string> ProjectFiles)
{
    /// <summary>
    /// Resolves <paramref name="path"/>. Supports a solution file, a single project file,
    /// or a directory scanned recursively for C# projects.
    /// </summary>
    public static ScanInput Resolve(string path)
    {
        string fullPath = Path.GetFullPath(path);

        if (Directory.Exists(fullPath))
        {
            IReadOnlyList<string> projects = Directory
                .EnumerateFiles(fullPath, "*.csproj", SearchOption.AllDirectories)
                .ToList();
            return new ScanInput(fullPath, Order(projects, fullPath));
        }

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Scan target not found: {path}", fullPath);
        }

        string extension = Path.GetExtension(fullPath);

        if (extension.Equals(".sln", StringComparison.OrdinalIgnoreCase))
        {
            string root = Path.GetDirectoryName(fullPath)!;
            IReadOnlyList<string> projects = SolutionParser.GetProjectPaths(fullPath)
                .Where(p => Path.GetExtension(p).Equals(".csproj", StringComparison.OrdinalIgnoreCase))
                .ToList();
            return new ScanInput(root, Order(projects, root));
        }

        if (extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            string root = Path.GetDirectoryName(fullPath)!;
            return new ScanInput(root, [fullPath]);
        }

        throw new ArgumentException(
            $"Unsupported scan target '{path}'. Expected a .sln, a .csproj, or a directory.",
            nameof(path));
    }

    // Deterministic order: by repo-relative path, ordinal. Same input => same output.
    private static IReadOnlyList<string> Order(IEnumerable<string> projectFiles, string rootDirectory) =>
        projectFiles
            .OrderBy(p => PathUtilities.ToRelative(rootDirectory, p), StringComparer.Ordinal)
            .ToList();
}
