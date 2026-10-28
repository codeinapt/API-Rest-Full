using Dapper;
using Inventory.Application.Movements.Ports;
using Inventory.Domain.Movements;

namespace Inventory.Infrastructure.Persistence.Write;

internal sealed class MovementWriteRepository(UnitOfWork unitOfWork) : IMovementWriteRepository
{
    private const string ExistsRelatedMovementSql = """
        SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.InventoryMovements WHERE Id = @RelatedMovementId AND ProductId = @ProductId) THEN 1 ELSE 0 END;
        """;

    private const string InsertSql = """
        INSERT INTO dbo.InventoryMovements (ProductId, Type, Delta, PreviousStock, ResultingStock, AdjustmentReason, Notes, RelatedMovementId, CreatedAt, CreatedBy)
        OUTPUT inserted.Id, inserted.ProductId, inserted.Type, inserted.Delta, inserted.PreviousStock, inserted.ResultingStock, inserted.AdjustmentReason,
               inserted.Notes, inserted.RelatedMovementId, inserted.CreatedAt, inserted.CreatedBy
        VALUES (@ProductId, @Type, @Delta, @PreviousStock, @ResultingStock, @AdjustmentReason, @Notes, @RelatedMovementId, @CreatedAt, @CreatedBy);
        """;

    public async Task<bool> ExistsRelatedMovementAsync(InventoryMovement adjustment, CancellationToken cancellationToken)
    {
        var parameters = new { adjustment.RelatedMovementId, adjustment.ProductId };
        var command = new CommandDefinition(ExistsRelatedMovementSql, parameters, unitOfWork.Transaction, cancellationToken: cancellationToken);

        return await unitOfWork.Connection.ExecuteScalarAsync<bool>(command);
    }

    public async Task<InventoryMovement> InsertAsync(InventoryMovement movement, CancellationToken cancellationToken)
    {
        var parameters = new
        {
            movement.ProductId,
            Type = (byte)movement.Type,
            movement.Delta,
            movement.PreviousStock,
            movement.ResultingStock,
            AdjustmentReason = (byte?)movement.AdjustmentReason,
            movement.Notes,
            movement.RelatedMovementId,
            movement.CreatedAt,
            movement.CreatedBy,
        };
        var command = new CommandDefinition(InsertSql, parameters, unitOfWork.Transaction, cancellationToken: cancellationToken);

        return await unitOfWork.Connection.QuerySingleAsync<InventoryMovement>(command);
    }
}
