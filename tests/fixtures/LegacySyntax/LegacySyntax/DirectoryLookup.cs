using System.DirectoryServices; // MIG4004 — Active Directory

namespace LegacySyntax
{
    public class DirectoryLookup
    {
        public object Find(string path) => new DirectoryEntry(path);
    }
}
