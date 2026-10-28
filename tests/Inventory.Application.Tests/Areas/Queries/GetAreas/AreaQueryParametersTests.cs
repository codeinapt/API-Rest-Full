using Inventory.Application.Areas.Queries.GetAreas;
using Inventory.Application.Common.Pagination;

namespace Inventory.Application.Tests.Areas.Queries.GetAreas;

public sealed class AreaQueryParametersTests
{
    [Fact]
    public void New_WhenNothingIsSpecified_DefaultsToFirstPageOfActiveAreas()
    {
        var parameters = new AreaQueryParameters();
        parameters.ShouldSatisfyAllConditions(
            () => parameters.Page.ShouldBe(PaginationParameters.FirstPage),
            () => parameters.PageSize.ShouldBe(PaginationParameters.DefaultPageSize),
            () => parameters.Status.ShouldBe(StatusFilter.Active));
    }
}
