namespace MigrationScan.Core.Tests;

/// <summary>
/// Locates the shared <c>tests/fixtures</c> directory at runtime by walking up from the
/// test assembly location. Avoids copying fixtures into every test project's output.
/// </summary>
internal static class Fixtures
{
    public static string Root { get; } = Locate();

    public static string Path(params string[] parts) =>
        System.IO.Path.Combine([Root, .. parts]);

    private static string Locate()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            string candidate = System.IO.Path.Combine(directory.FullName, "tests", "fixtures");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Could not locate tests/fixtures walking up from {AppContext.BaseDirectory}.");
    }
}
