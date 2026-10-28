using Inventory.Application.Common.Pagination;

namespace Inventory.Application.Products.Queries.GetProducts;

public sealed record ProductQueryParameters : PaginationParameters
{
    public int? CategoryId { get; init; }

    public StatusFilter Status { get; init; } = StatusFilter.Active;

    public string? Search { get; init; }

    public bool? LowStock { get; init; }

    public ProductSortField SortBy { get; init; } = ProductSortField.Name;

    public ProductSortDirection SortDirection { get; init; } = ProductSortDirection.Asc;
}
