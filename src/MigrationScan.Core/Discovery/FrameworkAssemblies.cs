namespace MigrationScan.Core.Discovery;

/// <summary>
/// Recognizes assembly names that ship with .NET Framework itself, so they can be told apart
/// from third-party assemblies resolved out of the GAC. Deliberately prefix-based rather than
/// an exhaustive list: the framework surface is large, and a false "this is framework" on an
/// unusual <c>System.*</c> name is cheaper than flooding every report with the BCL.
/// </summary>
public static class FrameworkAssemblies
{
    private static readonly string[] Prefixes =
    [
        "System", "mscorlib", "netstandard", "Microsoft.CSharp", "Microsoft.VisualBasic",
        "Microsoft.Win32", "PresentationCore", "PresentationFramework", "WindowsBase",
        "UIAutomationProvider", "UIAutomationTypes", "ReachFramework",
    ];

    /// <summary>
    /// True when <paramref name="simpleName"/> is a framework assembly name — an exact match on
    /// a known prefix, or a dotted child of one (<c>System.Web</c> under <c>System</c>).
    /// </summary>
    public static bool Contains(string simpleName) =>
        Prefixes.Any(prefix =>
            simpleName.Equals(prefix, StringComparison.OrdinalIgnoreCase)
            || simpleName.StartsWith(prefix + ".", StringComparison.OrdinalIgnoreCase));
}
