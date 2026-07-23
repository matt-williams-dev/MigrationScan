namespace MigrationScan.Core.Discovery;

/// <summary>
/// Path helpers that keep output deterministic and identical across operating systems:
/// paths are made relative to the scan root and always use forward slashes.
/// </summary>
public static class PathUtilities
{
    /// <summary>
    /// Returns <paramref name="fullPath"/> relative to <paramref name="baseDirectory"/>,
    /// using forward slashes regardless of the host OS.
    /// </summary>
    public static string ToRelative(string baseDirectory, string fullPath)
    {
        string relative = Path.GetRelativePath(baseDirectory, fullPath);
        return NormalizeSeparators(relative);
    }

    /// <summary>Replaces backslashes with forward slashes.</summary>
    public static string NormalizeSeparators(string path) => path.Replace('\\', '/');

    /// <summary>
    /// Resolves a solution-relative project path (which uses Windows separators) into an
    /// absolute path rooted at <paramref name="solutionDirectory"/>.
    /// </summary>
    public static string ResolveFromSolution(string solutionDirectory, string projectRelativePath)
    {
        string normalized = projectRelativePath.Replace('\\', Path.DirectorySeparatorChar);
        return Path.GetFullPath(Path.Combine(solutionDirectory, normalized));
    }
}
