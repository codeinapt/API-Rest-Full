using Inventory.Application.Common.Pagination;
using Inventory.Application.Movements.Dtos;
using Mediator;

namespace Inventory.Application.Movements.Queries.GetMovements;

public sealed record GetMovementsQuery(int ProductId, MovementQueryParameters Parameters) : IRequest<PagedResponse<MovementResponse>>;
