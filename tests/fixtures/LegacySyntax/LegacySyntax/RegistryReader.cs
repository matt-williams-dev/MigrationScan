using Microsoft.Win32; // MIG4002 — Windows Registry

namespace LegacySyntax
{
    public class RegistryReader
    {
        public object Read()
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Contoso");
            return key?.GetValue("Setting");
        }
    }
}
