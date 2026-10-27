using Inventory.Application.Common.Pagination;

namespace Inventory.Application.Areas.Queries.GetAreas;

public sealed record AreaQueryParameters : PaginationParameters
{
    public StatusFilter Status { get; init; } = StatusFilter.Active;
}
