namespace LegacySyntax
{
    public class QualifiedUsage
    {
        // Fully-qualified type, no `using` — exercises qualified-name recall (MIG7001).
        public object Connect(string connectionString) =>
            new System.Data.SqlClient.SqlConnection(connectionString);
    }
}
