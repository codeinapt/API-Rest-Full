using Inventory.Application.Common.Interfaces;
using Microsoft.Data.SqlClient;

namespace Inventory.Infrastructure.Persistence.Write;

internal sealed class UnitOfWork(SqlConnectionFactory connectionFactory) : IUnitOfWork, IAsyncDisposable
{
    private SqlConnection? connection;
    private SqlTransaction? transaction;

    public SqlConnection Connection => connection ?? throw TransactionNotStarted();

    public SqlTransaction Transaction => transaction ?? throw TransactionNotStarted();

    public async Task BeginTransactionAsync(CancellationToken cancellationToken)
    {
        if (transaction is not null)
        {
            throw new InvalidOperationException("A transaction is already in progress in this unit of work.");
        }
        connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        await Transaction.CommitAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (transaction is not null)
        {
            await transaction.DisposeAsync();
        }
        if (connection is not null)
        {
            await connection.DisposeAsync();
        }
    }

    private static InvalidOperationException TransactionNotStarted()
    {
        return new InvalidOperationException("BeginTransactionAsync must be called before using the unit of work.");
    }
}
