using System.Data.SqlClient; // MIG7001 — System.Data.SqlClient

namespace LegacySyntax
{
    public class Repository
    {
        public SqlConnection Connect(string connectionString) => new SqlConnection(connectionString);
    }
}
