using Microsoft.Data.SqlClient;

namespace Inventory.Infrastructure.Persistence.Write;

internal sealed class SqlConnectionFactory(string databaseConnectionString)
{
    public SqlConnection CreateConnection()
    {
        return new SqlConnection(databaseConnectionString);
    }
}
