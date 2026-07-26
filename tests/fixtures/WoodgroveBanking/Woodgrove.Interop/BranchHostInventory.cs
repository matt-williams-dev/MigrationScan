using System.Collections.Generic;
using System.Management;

namespace Woodgrove.Interop
{
    /// <summary>
    /// Inventories branch hardware over WMI. System.Management is a Windows-only surface
    /// (MIG4003).
    /// </summary>
    public static class BranchHostInventory
    {
        public static IEnumerable<string> ListDiskSerials()
        {
            var serials = new List<string>();

            using (var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_DiskDrive"))
            {
                foreach (ManagementObject drive in searcher.Get())
                {
                    serials.Add(drive["SerialNumber"] as string ?? string.Empty);
                }
            }

            return serials;
        }
    }
}
