using Inventory.Application.Common.Pagination;
using Inventory.Application.Movements.Dtos;
using Inventory.Application.Movements.Queries.GetMovements;

namespace Inventory.Application.Movements.Ports;

public interface IMovementReadRepository
{
    Task<PagedResponse<MovementResponse>> GetPageAsync(GetMovementsQuery query, CancellationToken cancellationToken);
}
