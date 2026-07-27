namespace MigrationScan.Core.Tests;

/// <summary>
/// Locates the repository root at runtime by walking up from the test assembly location,
/// for tests that assert against committed files outside the build output (docs, samples).
/// </summary>
internal static class Repo
{
    private const string RootMarker = "MigrationScan.slnx";

    public static string Root { get; } = Locate();

    private static string Locate()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, RootMarker)))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Could not locate {RootMarker} walking up from {AppContext.BaseDirectory}.");
    }
}
