using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace LegacySyntax
{
    public class Serializer
    {
        public void Save(Stream stream, object graph)
        {
            var formatter = new BinaryFormatter(); // MIG6001 — removed in .NET 9
            formatter.Serialize(stream, graph);
        }
    }
}
