using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;

namespace Woodgrove.Domain
{
    /// <summary>
    /// Reads the ledger over the Framework-era System.Data.SqlClient provider (MIG7001).
    /// </summary>
    public static class LedgerGateway
    {
        public static IEnumerable<string> ListAccounts()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["WoodgroveLedger"].ConnectionString;

            using (var connection = new SqlConnection(connectionString))
            using (var command = new SqlCommand("SELECT AccountNumber FROM dbo.Account", connection))
            {
                connection.Open();

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        yield return reader.GetString(0);
                    }
                }
            }
        }
    }
}
