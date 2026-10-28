using Inventory.Application.Common.Pagination;
using Inventory.Application.Products.Queries.GetProducts;

namespace Inventory.Application.Tests.Products.Queries.GetProducts;

public sealed class ProductQueryParametersTests
{
    [Fact]
    public void New_WhenNothingIsSpecified_DefaultsToFirstPageOfActiveProductsSortedByNameAscending()
    {
        var parameters = new ProductQueryParameters();
        parameters.ShouldSatisfyAllConditions(
            () => parameters.Page.ShouldBe(PaginationParameters.FirstPage),
            () => parameters.PageSize.ShouldBe(PaginationParameters.DefaultPageSize),
            () => parameters.CategoryId.ShouldBeNull(),
            () => parameters.Status.ShouldBe(StatusFilter.Active),
            () => parameters.Search.ShouldBeNull(),
            () => parameters.LowStock.ShouldBeNull(),
            () => parameters.SortBy.ShouldBe(ProductSortField.Name),
            () => parameters.SortDirection.ShouldBe(SortDirection.Asc));
    }
}
