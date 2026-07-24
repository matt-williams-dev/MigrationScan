using System.Diagnostics;

namespace LegacySyntax
{
    public class AuditLog
    {
        // MIG4005 — Windows Event Log
        public void Write(string message) => EventLog.WriteEntry("MyApp", message);
    }
}
