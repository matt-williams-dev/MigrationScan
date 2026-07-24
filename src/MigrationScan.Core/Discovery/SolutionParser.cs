using System.Text.RegularExpressions;

namespace MigrationScan.Core.Discovery;

/// <summary>
/// Hand-rolled <c>.sln</c> parser (spec §3: no MSBuild dependency). Extracts the project
/// references from the classic solution format without evaluating anything.
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
    /// Returns every project referenced by the solution (in order), with its type GUID —
    /// including non-C#/VB projects. Solution folders are skipped.
    /// </summary>
    public static IReadOnlyList<SolutionProjectEntry> GetProjects(string solutionFilePath)
    {
        string solutionDirectory = Path.GetDirectoryName(Path.GetFullPath(solutionFilePath))
            ?? throw new ArgumentException($"Could not determine directory for '{solutionFilePath}'.", nameof(solutionFilePath));

        List<SolutionProjectEntry> projects = [];

        foreach (string line in File.ReadLines(solutionFilePath))
        {
            Match match = ProjectLine().Match(line.Trim());
            if (!match.Success)
            {
                continue;
            }

            string typeGuid = match.Groups["type"].Value;
            if (string.Equals(typeGuid, SolutionFolderTypeGuid, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string absolutePath = PathUtilities.ResolveFromSolution(solutionDirectory, match.Groups["path"].Value);
            projects.Add(new SolutionProjectEntry(match.Groups["name"].Value, absolutePath, typeGuid));
        }

        return projects;
    }

    /// <summary>Absolute paths to the project files referenced by the solution, in order.</summary>
    public static IReadOnlyList<string> GetProjectPaths(string solutionFilePath) =>
        GetProjects(solutionFilePath).Select(p => p.AbsolutePath).ToList();
}
