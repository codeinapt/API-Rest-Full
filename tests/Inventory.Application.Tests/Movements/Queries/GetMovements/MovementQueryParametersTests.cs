using Inventory.Application.Common.Pagination;
using Inventory.Application.Movements.Queries.GetMovements;

namespace Inventory.Application.Tests.Movements.Queries.GetMovements;

public sealed class MovementQueryParametersTests
{
    [Fact]
    public void New_WhenNothingIsSpecified_DefaultsToFirstPageWithoutFilters()
    {
        var parameters = new MovementQueryParameters();
        parameters.ShouldSatisfyAllConditions(
            () => parameters.Page.ShouldBe(PaginationParameters.FirstPage),
            () => parameters.PageSize.ShouldBe(PaginationParameters.DefaultPageSize),
            () => parameters.Type.ShouldBeNull(),
            () => parameters.From.ShouldBeNull(),
            () => parameters.To.ShouldBeNull());
    }
}
