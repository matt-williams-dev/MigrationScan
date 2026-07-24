using System.Data.OleDb; // MIG7003 — OLE DB

namespace LegacySyntax
{
    public class OleDbGateway
    {
        public OleDbConnection Connect(string connectionString) => new OleDbConnection(connectionString);
    }
}
