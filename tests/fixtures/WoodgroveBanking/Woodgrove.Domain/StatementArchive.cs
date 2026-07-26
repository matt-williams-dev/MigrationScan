using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Web;

namespace Woodgrove.Domain
{
    /// <summary>
    /// Persists statement batches to disk. The on-disk format is a BinaryFormatter graph, so
    /// every archived file in the estate is tied to the serializer that wrote it (MIG6001).
    /// </summary>
    public static class StatementArchive
    {
        public static byte[] Serialize(string accountNumber)
        {
            var batch = LoadRecent(accountNumber);

            using (var buffer = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(buffer, batch);
                return buffer.ToArray();
            }
        }

        public static List<StatementLine> Deserialize(Stream source)
        {
            var formatter = new BinaryFormatter();
            return (List<StatementLine>)formatter.Deserialize(source);
        }

        public static List<StatementLine> LoadRecent(string accountNumber)
        {
            // Reaching for the ambient request context from a class library keeps this assembly
            // bound to System.Web and to IIS hosting.
            var tenant = HttpContext.Current?.Items["Tenant"] as string ?? "default";

            return new List<StatementLine>
            {
                new StatementLine { Account = accountNumber, Tenant = tenant, Amount = 0m, Posted = DateTime.MinValue }
            };
        }
    }

    [Serializable]
    public class StatementLine
    {
        public string Account { get; set; }

        public string Tenant { get; set; }

        public decimal Amount { get; set; }

        public DateTime Posted { get; set; }
    }
}
