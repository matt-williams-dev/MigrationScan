using System.Text.RegularExpressions;

namespace MigrationScan.Core.Discovery;

/// <summary>
/// Hand-rolled <c>.sln</c> parser (spec §3: no MSBuild dependency). Extracts the
/// project file references from the classic solution format without evaluating anything.
/// </summary>
public static partial class SolutionParser
{
    // Solution-folder nodes use this project-type GUID and carry no real file on disk.
    private const string SolutionFolderTypeGuid = "2150E333-8FDC-42A3-9474-1A3956D46DE8";

    [GeneratedRegex(
        """^Project\("\{(?<type>[^}]+)\}"\)\s*=\s*"(?<name>[^"]*)",\s*"(?<path>[^"]*)",\s*"\{(?<guid>[^}]+)\}"\s*$""",
        RegexOptions.CultureInvariant)]
    private static partial Regex ProjectLine();

    /// <summary>
    /// Returns absolute paths to the project files referenced by the solution, in the
    /// order they appear. Solution folders are skipped.
    /// </summary>
    public static IReadOnlyList<string> GetProjectPaths(string solutionFilePath)
    {
        string solutionDirectory = Path.GetDirectoryName(Path.GetFullPath(solutionFilePath))
            ?? throw new ArgumentException($"Could not determine directory for '{solutionFilePath}'.", nameof(solutionFilePath));

        List<string> projectPaths = [];

        foreach (string line in File.ReadLines(solutionFilePath))
        {
            Match match = ProjectLine().Match(line.Trim());
            if (!match.Success)
            {
                continue;
            }

            if (string.Equals(match.Groups["type"].Value, SolutionFolderTypeGuid, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string relativePath = match.Groups["path"].Value;
            projectPaths.Add(PathUtilities.ResolveFromSolution(solutionDirectory, relativePath));
        }

        return projectPaths;
    }
}
