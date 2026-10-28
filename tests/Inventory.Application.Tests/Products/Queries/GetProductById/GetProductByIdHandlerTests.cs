using Inventory.Application.Common.Exceptions;
using Inventory.Application.Products.Dtos;
using Inventory.Application.Products.Ports;
using Inventory.Application.Products.Queries.GetProductById;
using Inventory.Domain.Common;
using Inventory.Domain.Products;
using NSubstitute;

namespace Inventory.Application.Tests.Products.Queries.GetProductById;

public sealed class GetProductByIdHandlerTests
{
    private const int ProductId = 21;
    private static readonly GetProductByIdQuery Query = new(ProductId);
    private static readonly DateTime CreatedAt = new(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);

    private readonly IProductReadRepository productReadRepository = Substitute.For<IProductReadRepository>();
    private readonly GetProductByIdHandler handler;

    public GetProductByIdHandlerTests()
    {
        handler = new GetProductByIdHandler(productReadRepository);
    }

    [Fact]
    public async Task Handle_WhenProductExists_ReturnsIt()
    {
        var expectedProduct = new ProductResponse(ProductId, "DRL-001", "Drill", null, 3, "Tools", "TOOLS", 8, 5, false, true, null, null, CreatedAt, null);
        productReadRepository.GetByIdAsync(ProductId, Arg.Any<CancellationToken>()).Returns(expectedProduct);
        var product = await handler.Handle(Query, CancellationToken.None);
        product.ShouldBe(expectedProduct);
    }

    [Fact]
    public async Task Handle_WhenProductIsInactive_StillReturnsItWithItsDeactivation()
    {
        var deactivation = new ProductDeactivationResponse(DeactivationReason.Damaged, "Broken case", CreatedAt.AddDays(3), "operator-sub");
        var inactiveProduct = new ProductResponse(ProductId, "DRL-001", "Drill", null, 3, "Tools", "TOOLS", 8, 5, false, false, deactivation, null, CreatedAt, null);
        productReadRepository.GetByIdAsync(ProductId, Arg.Any<CancellationToken>()).Returns(inactiveProduct);
        var product = await handler.Handle(Query, CancellationToken.None);
        product.ShouldSatisfyAllConditions(
            () => product.IsActive.ShouldBeFalse(),
            () => product.Deactivation.ShouldBe(deactivation));
    }

    [Fact]
    public async Task Handle_WhenProductDoesNotExist_ThrowsProductNotFoundWithId()
    {
        productReadRepository.GetByIdAsync(ProductId, Arg.Any<CancellationToken>()).Returns((ProductResponse?)null);
        var exception = await Should.ThrowAsync<NotFoundException>(handler.Handle(Query, CancellationToken.None).AsTask());
        exception.Code.ShouldBe(ErrorCode.ProductNotFound);
        exception.MessageArguments.ShouldBe([ProductId]);
    }
}
