using System;
using System.Runtime.InteropServices;

namespace Woodgrove.Interop
{
    /// <summary>
    /// Talks to the branch cheque scanner. P/Invoke into a Windows system DLL is Windows
    /// lock-in rather than a blocker: it keeps working on net10.0-windows (MIG4013).
    /// </summary>
    internal static class ChequeScannerInterop
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateFile(
            string fileName, uint access, uint share, IntPtr security, uint disposition, uint flags, IntPtr template);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool RevertToSelf();

        // The vendor's own native library, not a Windows system DLL.
        [DllImport("litware_scanner.dll")]
        private static extern int OpenFeeder(int port);

        public static void Warm()
        {
            _ = CreateFile(@"\\.\SCANNER1", 0x80000000, 0, IntPtr.Zero, 3, 0, IntPtr.Zero);
            _ = RevertToSelf();
            _ = OpenFeeder(1);
        }
    }
}
