using Microsoft.Win32;

namespace Woodgrove.Interop
{
    /// <summary>
    /// Branch terminals are provisioned by writing their profile under HKLM. The Registry does
    /// not exist off Windows (MIG4002).
    /// </summary>
    public static class TerminalProfile
    {
        private const string ProfileKey = @"SOFTWARE\Woodgrove\Terminals";

        public static string ReadBranchCode()
        {
            using (var key = Registry.LocalMachine.OpenSubKey(ProfileKey))
            {
                return key?.GetValue("BranchCode") as string ?? "0000";
            }
        }

        public static void WriteLastTeller(string tellerId)
        {
            using (var key = Registry.LocalMachine.CreateSubKey(ProfileKey))
            {
                key.SetValue("LastTeller", tellerId, RegistryValueKind.String);
            }
        }
    }
}
