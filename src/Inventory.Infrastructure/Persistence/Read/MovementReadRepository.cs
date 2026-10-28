using Inventory.Application.Common.Pagination;
using Inventory.Application.Movements.Dtos;
using Inventory.Application.Movements.Ports;
using Inventory.Application.Movements.Queries.GetMovements;
using Inventory.Domain.Movements;
using Inventory.Infrastructure.Persistence.Read.Models;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence.Read;

internal sealed class MovementReadRepository(InventoryReadDbContext context) : IMovementReadRepository
{
    public async Task<PagedResponse<MovementResponse>> GetPageAsync(GetMovementsQuery query, CancellationToken cancellationToken)
    {
        var productMovements = context.InventoryMovements.Where(movement => movement.ProductId == query.ProductId);
        var filteredMovements = Filter(productMovements, query.Parameters);
        var totalCount = await filteredMovements.CountAsync(cancellationToken);
        var pageOfMovements = filteredMovements
            .OrderByDescending(movement => movement.CreatedAt)
            .ThenByDescending(movement => movement.Id)
            .Skip(query.Parameters.CalculateItemsToSkip())
            .Take(query.Parameters.PageSize);
        var items = await ProjectToResponse(pageOfMovements).ToListAsync(cancellationToken);

        return new PagedResponse<MovementResponse>(items, query.Parameters, totalCount);
    }

    private static IQueryable<InventoryMovementRow> Filter(IQueryable<InventoryMovementRow> movements, MovementQueryParameters parameters)
    {
        var movementsOfType = FilterByType(movements, parameters.Type);
        var movementsSinceFrom = FilterByFrom(movementsOfType, parameters.From);

        return FilterByTo(movementsSinceFrom, parameters.To);
    }

    private static IQueryable<InventoryMovementRow> FilterByType(IQueryable<InventoryMovementRow> movements, MovementType? type)
    {
        return type is null ? movements : movements.Where(movement => movement.Type == (byte)type.Value);
    }

    private static IQueryable<InventoryMovementRow> FilterByFrom(IQueryable<InventoryMovementRow> movements, DateTimeOffset? from)
    {
        return from is null ? movements : movements.Where(movement => movement.CreatedAt >= from.Value.UtcDateTime);
    }

    private static IQueryable<InventoryMovementRow> FilterByTo(IQueryable<InventoryMovementRow> movements, DateTimeOffset? to)
    {
        return to is null ? movements : movements.Where(movement => movement.CreatedAt <= to.Value.UtcDateTime);
    }

    private static IQueryable<MovementResponse> ProjectToResponse(IQueryable<InventoryMovementRow> movements)
    {
        return movements.Select(movement => new MovementResponse(
            movement.Id,
            movement.ProductId,
            (MovementType)movement.Type,
            movement.Delta,
            movement.PreviousStock,
            movement.ResultingStock,
            (AdjustmentReason?)movement.AdjustmentReason,
            movement.Notes,
            movement.RelatedMovementId,
            movement.CreatedAt,
            movement.CreatedBy));
    }
}
