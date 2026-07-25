using MigrationScan.Core.Discovery;

namespace MigrationScan.Core.Analysis;

/// <summary>
/// Resolves a scan target (a <c>.sln</c>, a <c>.csproj</c>/<c>.vbproj</c>, or a directory)
/// into the set of project files to analyze, any non-C#/VB projects that also need scoping,
/// and the root directory that output paths are reported relative to.
/// </summary>
/// <param name="RootDirectory">Absolute directory that output paths are relative to.</param>
/// <param name="ProjectFiles">Absolute paths to the C#/VB projects to analyze, ordered.</param>
/// <param name="OtherProjects">Non-C#/VB projects that need scoping separately.</param>
/// <param name="Solutions">Solution files discovered under the root, ordered. Empty unless a
/// directory was scanned.</param>
/// <param name="OrphanProjects">Analyzable projects no discovered solution references, ordered.
/// Always a subset of <paramref name="ProjectFiles"/> — they are scanned like any other.</param>
public sealed record ScanInput(
    string RootDirectory,
    IReadOnlyList<string> ProjectFiles,
    IReadOnlyList<SolutionProjectEntry> OtherProjects)
{
    public IReadOnlyList<string> Solutions { get; init; } = [];

    public IReadOnlyList<string> OrphanProjects { get; init; } = [];

    private static readonly string[] ProjectExtensions = [".csproj", ".vbproj"];

    /// <summary>
    /// Directories never worth walking: build output, restored packages, and tooling state.
    /// A project file under any of these is a build artifact or somebody else's source, and
    /// scanning it would attribute third-party code to the customer's estate.
    /// </summary>
    private static readonly string[] ExcludedDirectories =
        ["bin", "obj", "packages", "node_modules", ".git", ".vs", ".svn", "TestResults"];

    /// <summary>
    /// Resolves <paramref name="path"/>. Supports a solution file, a single project file,
    /// or a directory scanned recursively for solutions and projects.
    /// </summary>
    public static ScanInput Resolve(string path)
    {
        string fullPath = Path.GetFullPath(path);

        if (Directory.Exists(fullPath))
        {
            return ResolveDirectory(fullPath);
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
            return new ScanInput(root, Order(analyzable, root), others) { Solutions = [fullPath] };
        }

        if (IsProjectFile(fullPath))
        {
            return new ScanInput(Path.GetDirectoryName(fullPath)!, [fullPath], []);
        }

        throw new ArgumentException(
            $"Unsupported scan target '{path}'. Expected a .sln, a .csproj/.vbproj, or a directory.",
            nameof(path));
    }

    /// <summary>
    /// Walks a directory for the whole estate: every solution, every project.
    /// </summary>
    /// <remarks>
    /// Projects are the unit of truth and solutions are the grouping, not the other way round.
    /// A project is scanned because it exists on disk, so a project no solution references is
    /// still assessed — those are exactly the ones that surface late and blow an estimate.
    /// Solutions are still parsed, because they carry two things the file system does not: the
    /// project-type GUID that identifies a Silverlight or Web Site project, and references to
    /// projects that have since been deleted.
    /// </remarks>
    private static ScanInput ResolveDirectory(string root)
    {
        List<string> solutions = [];
        List<string> analyzableOnDisk = [];
        List<string> otherOnDisk = [];

        foreach (string file in EnumerateFiles(root))
        {
            string extension = Path.GetExtension(file);
            if (extension.Equals(".sln", StringComparison.OrdinalIgnoreCase))
            {
                solutions.Add(file);
            }
            else if (IsProjectFile(file))
            {
                analyzableOnDisk.Add(file);
            }
            else if (IsOtherProjectFile(file))
            {
                otherOnDisk.Add(file);
            }
        }

        // Parse every solution to learn what it claims. A solution that will not parse is not
        // fatal here — the projects underneath it were already found on disk.
        List<SolutionProjectEntry> solutionEntries = [];
        foreach (string solution in solutions)
        {
            try
            {
                solutionEntries.AddRange(SolutionParser.GetProjects(solution));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Xml.XmlException)
            {
                // Discovery is best-effort; the analyzer reports what it could not read.
            }
        }

        var claimed = new HashSet<string>(
            solutionEntries.Select(e => e.AbsolutePath), PathComparer);

        // Analyzable projects: everything on disk, plus anything a solution references that is
        // missing from disk but still inside the root — the analyzer turns those into the
        // "project file not found" warning rather than letting a broken solution pass silently.
        var analyzable = new HashSet<string>(analyzableOnDisk, PathComparer);
        foreach (SolutionProjectEntry entry in solutionEntries.Where(e => e.IsAnalyzable))
        {
            if (IsUnder(root, entry.AbsolutePath))
            {
                analyzable.Add(entry.AbsolutePath);
            }
        }

        // Non-C#/VB projects: the ones solutions declare (which carry a type GUID, so a
        // Silverlight or Web Site project is recognized as such), plus any found loose on disk.
        List<SolutionProjectEntry> others = solutionEntries
            .Where(e => !e.IsAnalyzable && IsUnder(root, e.AbsolutePath))
            .ToList();
        var otherPaths = new HashSet<string>(others.Select(e => e.AbsolutePath), PathComparer);
        foreach (string file in otherOnDisk.Where(f => !otherPaths.Contains(f)))
        {
            // No solution claims it, so there is no type GUID to read — the extension is all we
            // have, which is enough for every type identified by extension.
            others.Add(new SolutionProjectEntry(Path.GetFileNameWithoutExtension(file), file, TypeGuid: ""));
        }

        List<string> orphans = analyzable.Where(p => !claimed.Contains(p)).ToList();

        return new ScanInput(root, Order(analyzable, root), Order(others, root))
        {
            Solutions = Order(solutions, root),
            OrphanProjects = Order(orphans, root),
        };
    }

    /// <summary>
    /// Recursive file walk that skips build output, restored packages, and tooling state.
    /// Hand-rolled rather than <c>AllDirectories</c> so an excluded directory is never
    /// descended into, and an unreadable one is skipped instead of failing the scan.
    /// </summary>
    private static IEnumerable<string> EnumerateFiles(string directory)
    {
        IEnumerable<string> files;
        IEnumerable<string> subdirectories;
        try
        {
            files = Directory.EnumerateFiles(directory);
            subdirectories = Directory.EnumerateDirectories(directory);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or DirectoryNotFoundException or IOException)
        {
            yield break;
        }

        foreach (string file in files)
        {
            yield return file;
        }

        foreach (string subdirectory in subdirectories)
        {
            string name = Path.GetFileName(subdirectory);
            if (ExcludedDirectories.Contains(name, StringComparer.OrdinalIgnoreCase)
                || name.StartsWith('.'))
            {
                continue;
            }

            foreach (string file in EnumerateFiles(subdirectory))
            {
                yield return file;
            }
        }
    }

    // Path identity: case-insensitive on Windows and macOS, case-sensitive on Linux. Getting this
    // wrong would either double-count a project or silently drop one.
    private static StringComparer PathComparer =>
        OperatingSystem.IsLinux() ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;

    private static bool IsUnder(string root, string path) =>
        path.StartsWith(root + Path.DirectorySeparatorChar, PathComparer == StringComparer.Ordinal
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase);

    private static bool IsProjectFile(string filePath) =>
        ProjectExtensions.Contains(Path.GetExtension(filePath), StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Any MSBuild-style project that is not C#/VB — <c>.sqlproj</c>, <c>.wixproj</c>,
    /// <c>.rptproj</c>, and so on. Matched by the <c>proj</c> suffix rather than a fixed list so
    /// a project type nobody has enumerated yet is still surfaced as unassessed rather than
    /// silently ignored.
    /// </summary>
    private static bool IsOtherProjectFile(string filePath) =>
        Path.GetExtension(filePath).EndsWith("proj", StringComparison.OrdinalIgnoreCase)
        && !IsProjectFile(filePath);

    // Deterministic order: by repo-relative path, ordinal. Same input => same output.
    private static IReadOnlyList<string> Order(IEnumerable<string> projectFiles, string rootDirectory) =>
        projectFiles
            .OrderBy(p => PathUtilities.ToRelative(rootDirectory, p), StringComparer.Ordinal)
            .ToList();

    private static IReadOnlyList<SolutionProjectEntry> Order(
        IEnumerable<SolutionProjectEntry> entries, string rootDirectory) =>
        entries
            .OrderBy(e => PathUtilities.ToRelative(rootDirectory, e.AbsolutePath), StringComparer.Ordinal)
            .ToList();
}
