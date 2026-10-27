using Inventory.Application.Common.Pagination;

namespace Inventory.Application.Tests.Common.Pagination;

public sealed class PaginationParametersTests
{
    [Fact]
    public void Page_WhenNotProvided_DefaultsToFirstPage()
    {
        var pagination = new PaginationParameters();
        pagination.Page.ShouldBe(1);
    }

    [Fact]
    public void PageSize_WhenNotProvided_DefaultsToTwenty()
    {
        var pagination = new PaginationParameters();
        pagination.PageSize.ShouldBe(20);
    }

    [Theory]
    [InlineData(1, 20, 0)]
    [InlineData(2, 20, 20)]
    [InlineData(4, 25, 75)]
    public void CalculateItemsToSkip_ForPageAndPageSize_ReturnsItemsOnPreviousPages(int page, int pageSize, int expectedItemsToSkip)
    {
        var pagination = new PaginationParameters { Page = page, PageSize = pageSize };
        pagination.CalculateItemsToSkip().ShouldBe(expectedItemsToSkip);
    }
}
