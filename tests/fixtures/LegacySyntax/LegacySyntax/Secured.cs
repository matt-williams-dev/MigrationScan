using System.Security.Permissions;

namespace LegacySyntax
{
    public class Secured
    {
        // MIG6004 — Code Access Security attribute.
        [SecurityPermission(SecurityAction.Demand, UnmanagedCode = true)]
        public void DoPrivilegedWork()
        {
        }
    }
}
