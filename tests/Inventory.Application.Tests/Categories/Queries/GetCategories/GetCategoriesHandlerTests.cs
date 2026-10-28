using Inventory.Application.Categories.Dtos;
using Inventory.Application.Categories.Ports;
using Inventory.Application.Categories.Queries.GetCategories;
using Inventory.Application.Common.Pagination;
using NSubstitute;

namespace Inventory.Application.Tests.Categories.Queries.GetCategories;

public sealed class GetCategoriesHandlerTests
{
    private readonly ICategoryReadRepository categoryReadRepository = Substitute.For<ICategoryReadRepository>();
    private readonly GetCategoriesHandler handler;

    public GetCategoriesHandlerTests()
    {
        handler = new GetCategoriesHandler(categoryReadRepository);
    }

    [Fact]
    public async Task Handle_ReturnsThePageReadWithTheQueryParameters()
    {
        var parameters = new CategoryQueryParameters { Page = 2, PageSize = 5, AreaId = 7, Status = StatusFilter.All };
        var expectedPage = new PagedResponse<CategoryResponse>([new CategoryResponse(11, 7, "WH", "Tools", false)], parameters, 6);
        categoryReadRepository.GetPageAsync(parameters, Arg.Any<CancellationToken>()).Returns(expectedPage);
        var page = await handler.Handle(new GetCategoriesQuery(parameters), CancellationToken.None);
        page.ShouldBeSameAs(expectedPage);
    }
}
