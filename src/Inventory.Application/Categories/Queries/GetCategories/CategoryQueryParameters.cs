using Inventory.Application.Common.Pagination;

namespace Inventory.Application.Categories.Queries.GetCategories;

public sealed record CategoryQueryParameters : PaginationParameters
{
    public int? AreaId { get; init; }

    public StatusFilter Status { get; init; } = StatusFilter.Active;
}
