using Inventory.Application.Common.Pagination;
using Inventory.Application.Products.Dtos;
using Inventory.Application.Products.Ports;
using Inventory.Application.Products.Queries.GetProducts;
using NSubstitute;

namespace Inventory.Application.Tests.Products.Queries.GetProducts;

public sealed class GetProductsHandlerTests
{
    private readonly IProductReadRepository productReadRepository = Substitute.For<IProductReadRepository>();
    private readonly GetProductsHandler handler;

    public GetProductsHandlerTests()
    {
        handler = new GetProductsHandler(productReadRepository);
    }

    [Fact]
    public async Task Handle_ReturnsThePageReadWithTheQueryParameters()
    {
        var parameters = new ProductQueryParameters { Page = 2, PageSize = 5, CategoryId = 3, Search = "drill", LowStock = true, SortBy = ProductSortField.Stock, SortDirection = SortDirection.Desc };
        var product = new ProductResponse(21, "DRL-001", "Drill", null, 3, "Tools", "TOOLS", 2, 5, true, true, null, null, DateTime.UtcNow, null);
        var expectedPage = new PagedResponse<ProductResponse>([product], parameters, 6);
        productReadRepository.GetPageAsync(parameters, Arg.Any<CancellationToken>()).Returns(expectedPage);
        var page = await handler.Handle(new GetProductsQuery(parameters), CancellationToken.None);
        page.ShouldBeSameAs(expectedPage);
    }
}
