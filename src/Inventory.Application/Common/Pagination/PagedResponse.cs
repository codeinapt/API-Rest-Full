namespace Inventory.Application.Common.Pagination;

public sealed record PagedResponse<T>
{
    public PagedResponse(IReadOnlyList<T> items, PaginationParameters pagination, int totalCount)
    {
        Items = items;
        Page = pagination.Page;
        PageSize = pagination.PageSize;
        TotalCount = totalCount;
        TotalPages = (totalCount + pagination.PageSize - 1) / pagination.PageSize;
    }

    public IReadOnlyList<T> Items { get; }

    public int Page { get; }

    public int PageSize { get; }

    public int TotalCount { get; }

    public int TotalPages { get; }
}
