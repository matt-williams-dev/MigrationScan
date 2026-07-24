using System.Data.Linq; // MIG7006 — LINQ to SQL

namespace LegacySyntax
{
    public class DataContextFactory
    {
        public DataContext Create(string connectionString) => new DataContext(connectionString);
    }
}
