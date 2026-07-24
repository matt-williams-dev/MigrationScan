namespace MigrationScan.Core.Models;

/// <summary>
/// Whether a rule's finding is a problem on any target, or only when moving off Windows.
///
/// Modern .NET can target Windows (`net10.0-windows`), where COM interop, P/Invoke to Win32,
/// the Registry, WMI, and similar continue to work. Those APIs are "Windows lock-in": they
/// only become migration cost when the target is cross-platform. A rule marked
/// <see cref="Windows"/> is downgraded and taken out of the effort rollup when the scan
/// target is a Windows TFM (spec §5, portability awareness).
/// </summary>
public enum RulePlatform
{
    /// <summary>
    /// The finding is a problem regardless of target — the API is gone everywhere on modern
    /// .NET (WebForms, BinaryFormatter, Remoting, …). This is the default.
    /// </summary>
    Any,

    /// <summary>
    /// The finding is a Windows lock-in: supported on `net-windows`, unavailable elsewhere.
    /// Only counts as migration cost when the scan target is cross-platform.
    /// </summary>
    Windows,
}
