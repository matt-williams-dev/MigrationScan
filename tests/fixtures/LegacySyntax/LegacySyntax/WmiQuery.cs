using System.Management; // MIG4003 — WMI

namespace LegacySyntax
{
    public class WmiQuery
    {
        public object Run() => new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem").Get();
    }
}
