namespace MigrationScan.Core.Models;

/// <summary>
/// Interprets a target framework moniker for portability purposes. A TFM with a
/// <c>-windows</c> platform suffix (e.g. <c>net10.0-windows</c>, <c>net8.0-windows10.0.19041</c>)
/// means the migration stays on Windows, so Windows lock-in findings are not migration cost.
/// </summary>
public static class TargetPlatform
{
    /// <summary>The platform suffix that marks a Windows-targeting TFM.</summary>
    private const string WindowsSuffix = "-windows";

    /// <summary>
    /// True when <paramref name="targetFramework"/> is a Windows-targeting TFM. Matches the
    /// <c>-windows</c> platform suffix anywhere after the framework part, case-insensitively.
    /// </summary>
    public static bool IsWindows(string targetFramework) =>
        targetFramework.Contains(WindowsSuffix, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The framework part with any platform suffix removed: <c>net10.0-windows10.0.19041</c>
    /// becomes <c>net10.0</c>. A TFM with no suffix is returned unchanged.
    /// </summary>
    public static string WithoutPlatform(string targetFramework)
    {
        int suffix = targetFramework.IndexOf(WindowsSuffix, StringComparison.OrdinalIgnoreCase);
        return suffix < 0 ? targetFramework : targetFramework[..suffix];
    }

    /// <summary>
    /// The two portability stances for one framework version — the pair a combined report covers.
    /// The platform axis is independent of the framework version, so a caller picks the version
    /// (<c>net10.0</c> vs <c>net8.0</c>) and always gets both stances.
    /// </summary>
    /// <remarks>
    /// A Windows TFM is preserved verbatim rather than rebuilt, so an OS-version-qualified target
    /// (<c>net8.0-windows10.0.19041</c>) keeps its qualifier instead of being flattened.
    /// </remarks>
    public static (string CrossPlatform, string Windows) Stances(string targetFramework) =>
        IsWindows(targetFramework)
            ? (WithoutPlatform(targetFramework), targetFramework)
            : (targetFramework, targetFramework + WindowsSuffix);
}
