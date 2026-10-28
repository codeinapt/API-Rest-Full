using Dapper;
using Microsoft.Data.SqlClient;

namespace Inventory.IntegrationTests.Common;

internal sealed class ProductRowLock : IAsyncDisposable
{
    private const string LockProductSql = "SELECT Id FROM dbo.Products WITH (UPDLOCK, HOLDLOCK) WHERE Id = @ProductId;";

    private const string CountLockWaitersSql = """
        SELECT COUNT(*) FROM sys.dm_exec_requests
        WHERE database_id = DB_ID() AND session_id <> @@SPID AND wait_type LIKE 'LCK[_]M[_]%';
        """;

    private static readonly TimeSpan LockWaitersTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan PollingInterval = TimeSpan.FromMilliseconds(50);

    private readonly SqlConnection connection;
    private readonly SqlTransaction transaction;

    private ProductRowLock(SqlConnection connection, SqlTransaction transaction)
    {
        this.connection = connection;
        this.transaction = transaction;
    }

    public static async Task<ProductRowLock> AcquireAsync(SqlServerFixture fixture, int productId)
    {
        var connection = new SqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
        await connection.ExecuteAsync(LockProductSql, new { ProductId = productId }, transaction);

        return new ProductRowLock(connection, transaction);
    }

    public async Task WaitForLockWaitersAsync(int expectedLockWaiters)
    {
        using var timeout = new CancellationTokenSource(LockWaitersTimeout);
        while (await CountLockWaitersAsync() < expectedLockWaiters)
        {
            await Task.Delay(PollingInterval, timeout.Token);
        }
    }

    public async Task ReleaseAsync()
    {
        await transaction.RollbackAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await transaction.DisposeAsync();
        await connection.DisposeAsync();
    }

    private async Task<int> CountLockWaitersAsync()
    {
        return await connection.ExecuteScalarAsync<int>(CountLockWaitersSql, transaction: transaction);
    }
}
