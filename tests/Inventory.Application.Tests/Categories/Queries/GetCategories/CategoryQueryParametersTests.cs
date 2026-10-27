using Inventory.Application.Categories.Queries.GetCategories;
using Inventory.Application.Common.Pagination;

namespace Inventory.Application.Tests.Categories.Queries.GetCategories;

public sealed class CategoryQueryParametersTests
{
    [Fact]
    public void New_WhenNothingIsSpecified_DefaultsToFirstPageOfActiveCategoriesOfAnyArea()
    {
        var parameters = new CategoryQueryParameters();
        parameters.ShouldSatisfyAllConditions(
            () => parameters.Page.ShouldBe(PaginationParameters.FirstPage),
            () => parameters.PageSize.ShouldBe(PaginationParameters.DefaultPageSize),
            () => parameters.AreaId.ShouldBeNull(),
            () => parameters.Status.ShouldBe(StatusFilter.Active));
    }
}
