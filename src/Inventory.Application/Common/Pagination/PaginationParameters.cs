namespace Inventory.Application.Common.Pagination;

public record PaginationParameters
{
    public const int FirstPage = 1;
    public const int DefaultPageSize = 20;
    public const int MinimumPageSize = 1;
    public const int MaximumPageSize = 100;

    public int Page { get; init; } = FirstPage;

    public int PageSize { get; init; } = DefaultPageSize;

    public int CalculateItemsToSkip()
    {
        return (Page - FirstPage) * PageSize;
    }
}
