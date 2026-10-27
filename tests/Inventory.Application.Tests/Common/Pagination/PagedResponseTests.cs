using Inventory.Application.Common.Pagination;

namespace Inventory.Application.Tests.Common.Pagination;

public sealed class PagedResponseTests
{
    private static readonly string[] PageItems = ["FOOD", "CLEAN"];

    [Fact]
    public void Constructor_WithItemsAndPagination_ExposesItemsAndRequestedPage()
    {
        var response = new PagedResponse<string>(PageItems, new PaginationParameters { Page = 3, PageSize = 2 }, 10);
        response.ShouldSatisfyAllConditions(
            () => response.Items.ShouldBe(PageItems),
            () => response.Page.ShouldBe(3),
            () => response.PageSize.ShouldBe(2),
            () => response.TotalCount.ShouldBe(10));
    }

    [Theory]
    [InlineData(0, 20, 0)]
    [InlineData(1, 20, 1)]
    [InlineData(20, 20, 1)]
    [InlineData(21, 20, 2)]
    [InlineData(100, 25, 4)]
    public void TotalPages_ForTotalCountAndPageSize_RoundsUpToWholePages(int totalCount, int pageSize, int expectedTotalPages)
    {
        var response = new PagedResponse<string>([], new PaginationParameters { PageSize = pageSize }, totalCount);
        response.TotalPages.ShouldBe(expectedTotalPages);
    }
}
