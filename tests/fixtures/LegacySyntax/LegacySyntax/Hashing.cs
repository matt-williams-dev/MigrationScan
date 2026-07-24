using System.Security.Cryptography;

namespace LegacySyntax
{
    public class Hashing
    {
        // MIG6005 — obsolete cryptography types
        public byte[] Hash(byte[] data)
        {
            using var sha = new SHA1Managed();
            return sha.ComputeHash(data);
        }
    }
}
