namespace MigrationScan.Core.Models;

/// <summary>
/// Interprets a target framework moniker for portability purposes. A TFM with a
/// <c>-windows</c> platform suffix (e.g. <c>net10.0-windows</c>, <c>net8.0-windows10.0.19041</c>)
/// means the migration stays on Windows, so Windows lock-in findings are not migration cost.
/// </summary>
public static class TargetPlatform
{
    /// <summary>
    /// True when <paramref name="targetFramework"/> is a Windows-targeting TFM. Matches the
    /// <c>-windows</c> platform suffix anywhere after the framework part, case-insensitively.
    /// </summary>
    public static bool IsWindows(string targetFramework) =>
        targetFramework.Contains("-windows", StringComparison.OrdinalIgnoreCase);
}
