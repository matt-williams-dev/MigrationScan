using MigrationScan.Core.Engine;
using MigrationScan.Core.Models;

namespace MigrationScan.Core.Rules;

/// <summary>
/// MIG4013 — P/Invoke to a Windows system DLL. Tier 2 (probable): a <c>[DllImport("kernel32.dll")]</c>
/// (or user32/advapi32/…) call. Works on modern .NET when targeting Windows (net-windows); it is a
/// Windows lock-in and won't run cross-platform.
/// </summary>
public sealed class Mig4013PInvokeWindows : SyntaxRule
{
    public const string Id = "MIG4013";

    // Common Windows system libraries reached via P/Invoke.
    private static readonly HashSet<string> WindowsSystemDlls = new(StringComparer.OrdinalIgnoreCase)
    {
        "kernel32", "user32", "advapi32", "gdi32", "gdiplus", "shell32", "ole32", "oleaut32",
        "comctl32", "comdlg32", "winspool", "winspool.drv", "ws2_32", "wininet", "crypt32",
        "secur32", "netapi32", "setupapi", "iphlpapi", "psapi", "dbghelp", "version", "winmm", "ntdll",
    };

    public Mig4013PInvokeWindows(RuleMetadata metadata) : base(metadata)
    {
    }

    protected override IEnumerable<Finding> AnalyzeSource(SourceFile source, AnalysisContext context)
    {
        var root = source.SyntaxTree.GetRoot();
        foreach ((int line, string? library) in SyntaxScan.AttributesWithStringArg(root, "DllImport"))
        {
            if (library is null || !IsWindowsSystemDll(library))
            {
                continue;
            }

            yield return Report(
                context,
                source,
                $"P/Invokes the Windows system library '{library}' via [DllImport]. This works on modern .NET " +
                "only when targeting Windows (net-windows); it is a Windows lock-in and won't run cross-platform.",
                line);
        }
    }

    private static bool IsWindowsSystemDll(string library)
    {
        string name = Path.GetFileNameWithoutExtension(library.Trim());
        return WindowsSystemDlls.Contains(name);
    }
}
