using Inventory.Application.Common.Pagination;
using Inventory.Domain.Movements;

namespace Inventory.Application.Movements.Queries.GetMovements;

public sealed record MovementQueryParameters : PaginationParameters
{
    public MovementType? Type { get; init; }

    public DateTimeOffset? From { get; init; }

    public DateTimeOffset? To { get; init; }
}
