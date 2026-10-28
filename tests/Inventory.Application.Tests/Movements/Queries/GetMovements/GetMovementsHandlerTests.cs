using Inventory.Application.Common.Exceptions;
using Inventory.Application.Common.Pagination;
using Inventory.Application.Movements.Dtos;
using Inventory.Application.Movements.Ports;
using Inventory.Application.Movements.Queries.GetMovements;
using Inventory.Application.Products.Dtos;
using Inventory.Application.Products.Ports;
using Inventory.Domain.Common;
using Inventory.Domain.Movements;
using Inventory.Domain.Products;
using NSubstitute;

namespace Inventory.Application.Tests.Movements.Queries.GetMovements;

public sealed class GetMovementsHandlerTests
{
    private const int ProductId = 21;
    private static readonly DateTime CreatedAt = new(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);
    private static readonly GetMovementsQuery Query = new(ProductId, new MovementQueryParameters { Type = MovementType.In });
    private static readonly MovementResponse Movement = new(5, ProductId, MovementType.In, 10, 0, 10, null, null, null, CreatedAt, "operator-sub");

    private readonly IProductReadRepository productReadRepository = Substitute.For<IProductReadRepository>();
    private readonly IMovementReadRepository movementReadRepository = Substitute.For<IMovementReadRepository>();
    private readonly GetMovementsHandler handler;

    public GetMovementsHandlerTests()
    {
        handler = new GetMovementsHandler(productReadRepository, movementReadRepository);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_WhenProductExistsActiveOrNot_ReturnsThePageOfItsMovements(bool isActive)
    {
        var expectedPage = new PagedResponse<MovementResponse>([Movement], Query.Parameters, 1);
        productReadRepository.GetByIdAsync(ProductId, Arg.Any<CancellationToken>()).Returns(CreateProduct(isActive));
        movementReadRepository.GetPageAsync(Query, Arg.Any<CancellationToken>()).Returns(expectedPage);
        var page = await handler.Handle(Query, CancellationToken.None);
        page.ShouldBeSameAs(expectedPage);
    }

    [Fact]
    public async Task Handle_WhenProductDoesNotExist_ThrowsProductNotFoundWithoutReadingMovements()
    {
        productReadRepository.GetByIdAsync(ProductId, Arg.Any<CancellationToken>()).Returns((ProductResponse?)null);
        var exception = await Should.ThrowAsync<NotFoundException>(handler.Handle(Query, CancellationToken.None).AsTask());
        exception.ShouldSatisfyAllConditions(
            () => exception.Code.ShouldBe(ErrorCode.ProductNotFound),
            () => exception.MessageArguments.ShouldBe([ProductId]));
        await movementReadRepository.DidNotReceive().GetPageAsync(Arg.Any<GetMovementsQuery>(), Arg.Any<CancellationToken>());
    }

    private static ProductResponse CreateProduct(bool isActive)
    {
        var deactivation = isActive ? null : new ProductDeactivationResponse(DeactivationReason.Damaged, null, CreatedAt.AddDays(1), "operator-sub");

        return new ProductResponse(ProductId, "DRL-001", "Drill", null, 3, "Tools", "TOOLS", 10, 5, false, isActive, deactivation, null, CreatedAt, null);
    }
}
