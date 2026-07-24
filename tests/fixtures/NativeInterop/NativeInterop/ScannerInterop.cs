using System;
using System.Runtime.InteropServices;

namespace NativeInterop;

/// <summary>
/// Wraps a couple of Win32 calls. P/Invoke to a Windows system DLL is a Windows lock-in
/// (MIG4013): supported on net-windows, but no cross-platform successor.
/// </summary>
internal static class ScannerInterop
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr CreateEvent(IntPtr attributes, bool manualReset, bool initialState, string? name);

    // user32 is also a Windows system library — should be flagged too.
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    // Not a known Windows system DLL — a bespoke native library. Should NOT be flagged by MIG4013.
    [DllImport("scanner_sdk.dll")]
    private static extern int OpenDevice(int channel);

    public static void Ping()
    {
        _ = CreateEvent(IntPtr.Zero, true, false, "Local\\scan");
        _ = SetForegroundWindow(IntPtr.Zero);
        _ = OpenDevice(0);
    }
}
