using Inventory.Application.Common.Exceptions;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Movements.Commands.RegisterMovement;
using Inventory.Application.Movements.Dtos;
using Inventory.Application.Movements.Ports;
using Inventory.Application.Products.Ports;
using Inventory.Application.Tests.Common;
using Inventory.Domain.Areas;
using Inventory.Domain.Categories;
using Inventory.Domain.Common;
using Inventory.Domain.Movements;
using Inventory.Domain.Products;
using NSubstitute;

namespace Inventory.Application.Tests.Movements.Commands.RegisterMovement;

public sealed class RegisterMovementHandlerTests
{
    private const int ProductId = 21;
    private const int CategoryId = 7;
    private const int SnapshotStock = 12;
    private const int StockAtUpdate = 15;
    private const int Quantity = 5;
    private const int StockAfterConcurrentOutbound = 2;
    private const long MovementId = 301;
    private const string CurrentUserSubject = "operator-1";
    private const string Notes = "Supplier delivery";
    private static readonly DateTime CreationTime = new(2026, 10, 27, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime CurrentTime = new(2026, 10, 28, 11, 30, 0, DateTimeKind.Utc);

    private readonly IProductWriteRepository productWriteRepository = Substitute.For<IProductWriteRepository>();
    private readonly IMovementWriteRepository movementWriteRepository = Substitute.For<IMovementWriteRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly Product product = CreateActiveProduct(SnapshotStock);
    private readonly RegisterMovementHandler handler;

    public RegisterMovementHandlerTests()
    {
        var clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(CurrentTime);
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.Subject.Returns(CurrentUserSubject);
        productWriteRepository.GetStockSnapshotAsync(ProductId, Arg.Any<CancellationToken>()).Returns(product);
        productWriteRepository.ApplyDeltaAsync(Arg.Any<InventoryMovement>(), Arg.Any<CancellationToken>()).Returns(StockAtUpdate);
        movementWriteRepository.InsertAsync(Arg.Any<InventoryMovement>(), Arg.Any<CancellationToken>())
            .Returns(call => PersistedIdentity.Assign(call.Arg<InventoryMovement>(), MovementId));
        handler = new RegisterMovementHandler(productWriteRepository, movementWriteRepository, unitOfWork, clock, currentUser);
    }

    [Fact]
    public async Task Handle_WithInbound_AppliesPositiveDeltaToProduct()
    {
        await handler.Handle(CreateCommand(MovementType.In, Quantity), CancellationToken.None);
        await productWriteRepository.Received(1).ApplyDeltaAsync(
            Arg.Is<InventoryMovement>(movement => movement.ProductId == ProductId && movement.Type == MovementType.In && movement.Delta == Quantity),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithOutbound_AppliesNegativeDeltaToProduct()
    {
        await handler.Handle(CreateCommand(MovementType.Out, Quantity), CancellationToken.None);
        await productWriteRepository.Received(1).ApplyDeltaAsync(
            Arg.Is<InventoryMovement>(movement => movement.ProductId == ProductId && movement.Type == MovementType.Out && movement.Delta == -Quantity),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithOutbound_InsertsMovementWithStockFromUpdateOutputCurrentTimeAndUser()
    {
        await handler.Handle(CreateCommand(MovementType.Out, Quantity), CancellationToken.None);
        await movementWriteRepository.Received(1).InsertAsync(
            Arg.Is<InventoryMovement>(movement => movement.PreviousStock == StockAtUpdate && movement.ResultingStock == StockAtUpdate - Quantity
                && movement.Notes == Notes && movement.CreatedAt == CurrentTime && movement.CreatedBy == CurrentUserSubject),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithInbound_ReturnsPersistedMovementResponse()
    {
        var response = await handler.Handle(CreateCommand(MovementType.In, Quantity), CancellationToken.None);
        response.ShouldBe(new MovementResponse(
            MovementId, ProductId, MovementType.In, Quantity, StockAtUpdate, StockAtUpdate + Quantity, null, Notes, null, CurrentTime, CurrentUserSubject));
    }

    [Fact]
    public async Task Handle_WithValidMovement_ReadsSnapshotUpdatesStockAndInsertsMovementInsideTransaction()
    {
        await handler.Handle(CreateCommand(MovementType.Out, Quantity), CancellationToken.None);
        Received.InOrder(() =>
        {
            unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>());
            productWriteRepository.GetStockSnapshotAsync(ProductId, Arg.Any<CancellationToken>());
            productWriteRepository.ApplyDeltaAsync(Arg.Any<InventoryMovement>(), Arg.Any<CancellationToken>());
            movementWriteRepository.InsertAsync(Arg.Any<InventoryMovement>(), Arg.Any<CancellationToken>());
            unitOfWork.CommitAsync(Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task Handle_WhenProductDoesNotExist_ThrowsProductNotFoundWithProductId()
    {
        productWriteRepository.GetStockSnapshotAsync(ProductId, Arg.Any<CancellationToken>()).Returns((Product?)null);
        var exception = await Should.ThrowAsync<NotFoundException>(handler.Handle(CreateCommand(MovementType.In, Quantity), CancellationToken.None).AsTask());
        exception.Code.ShouldBe(ErrorCode.ProductNotFound);
        exception.MessageArguments.ShouldBe([ProductId]);
    }

    [Theory]
    [InlineData(MovementType.In)]
    [InlineData(MovementType.Out)]
    public async Task Handle_WhenProductIsInactive_ThrowsProductInactiveWithoutTouchingStock(MovementType type)
    {
        product.Deactivate(new ProductDeactivation(DeactivationReason.Expired, null, CreationTime, CurrentUserSubject));
        var exception = await Should.ThrowAsync<DomainException>(handler.Handle(CreateCommand(type, Quantity), CancellationToken.None).AsTask());
        exception.Code.ShouldBe(ErrorCode.ProductInactive);
        exception.MessageArguments.ShouldBe([ProductId]);
        await productWriteRepository.DidNotReceive().ApplyDeltaAsync(Arg.Any<InventoryMovement>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenOutboundExceedsSnapshotStock_ThrowsInsufficientStockWithoutTouchingStock()
    {
        var exception = await Should.ThrowAsync<DomainException>(handler.Handle(CreateCommand(MovementType.Out, SnapshotStock + 1), CancellationToken.None).AsTask());
        exception.Code.ShouldBe(ErrorCode.InsufficientStock);
        exception.MessageArguments.ShouldBe([SnapshotStock, SnapshotStock + 1]);
        await productWriteRepository.DidNotReceive().ApplyDeltaAsync(Arg.Any<InventoryMovement>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenConditionalUpdateAffectsNoRowsAndProductIsStillActive_ThrowsInsufficientStockWithStockAfterConcurrentOutbound()
    {
        ArrangeConcurrentChange(CreateActiveProduct(StockAfterConcurrentOutbound));
        var exception = await Should.ThrowAsync<DomainException>(handler.Handle(CreateCommand(MovementType.Out, Quantity), CancellationToken.None).AsTask());
        exception.Code.ShouldBe(ErrorCode.InsufficientStock);
        exception.MessageArguments.ShouldBe([StockAfterConcurrentOutbound, Quantity]);
    }

    [Fact]
    public async Task Handle_WhenConditionalUpdateAffectsNoRowsAndProductWasDeactivated_ThrowsProductInactive()
    {
        var deactivatedProduct = CreateActiveProduct(SnapshotStock);
        deactivatedProduct.Deactivate(new ProductDeactivation(DeactivationReason.Recalled, null, CurrentTime, CurrentUserSubject));
        ArrangeConcurrentChange(deactivatedProduct);
        var exception = await Should.ThrowAsync<DomainException>(handler.Handle(CreateCommand(MovementType.In, Quantity), CancellationToken.None).AsTask());
        exception.Code.ShouldBe(ErrorCode.ProductInactive);
        exception.MessageArguments.ShouldBe([ProductId]);
    }

    [Fact]
    public async Task Handle_WhenConditionalUpdateAffectsNoRows_NeitherInsertsMovementNorCommits()
    {
        ArrangeConcurrentChange(CreateActiveProduct(0));
        await Should.ThrowAsync<DomainException>(handler.Handle(CreateCommand(MovementType.Out, Quantity), CancellationToken.None).AsTask());
        await movementWriteRepository.DidNotReceive().InsertAsync(Arg.Any<InventoryMovement>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    private static RegisterMovementCommand CreateCommand(MovementType type, int quantity)
    {
        return new RegisterMovementCommand(ProductId, new RegisterMovementRequest(type, quantity, Notes));
    }

    private static Product CreateActiveProduct(int stock)
    {
        var area = Area.Create("FOOD", "Food", CreationTime);
        var category = PersistedIdentity.Assign(Category.Create(area, "Dairy", CreationTime), CategoryId);
        var product = PersistedIdentity.Assign(Product.Create(category, "MILK-001", new ProductDetails("Whole milk", null, 4), CreationTime), ProductId);
        typeof(Product).GetProperty(nameof(Product.Stock))!.SetValue(product, stock);

        return product;
    }

    private void ArrangeConcurrentChange(Product productAfterChange)
    {
        productWriteRepository.ApplyDeltaAsync(Arg.Any<InventoryMovement>(), Arg.Any<CancellationToken>()).Returns((int?)null);
        productWriteRepository.GetStockSnapshotAsync(ProductId, Arg.Any<CancellationToken>()).Returns(product, productAfterChange);
    }
}
