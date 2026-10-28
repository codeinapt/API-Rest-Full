using Inventory.Application.Common.Authorization;
using Inventory.Application.Common.Exceptions;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Movements.Commands.RegisterAdjustment;
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

namespace Inventory.Application.Tests.Movements.Commands.RegisterAdjustment;

public sealed class RegisterAdjustmentHandlerTests
{
    private const int ProductId = 21;
    private const int CategoryId = 7;
    private const int Stock = 12;
    private const int Delta = -5;
    private const long RelatedMovementId = 250;
    private const long MovementId = 302;
    private const string AreaCode = "FOOD";
    private const string OtherAreaCode = "CLEAN";
    private const string CurrentUserSubject = "supervisor-1";
    private const string Notes = "Broken packaging";
    private static readonly DateTime CreationTime = new(2026, 10, 27, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime CurrentTime = new(2026, 10, 28, 11, 30, 0, DateTimeKind.Utc);

    private readonly IProductWriteRepository productWriteRepository = Substitute.For<IProductWriteRepository>();
    private readonly IMovementWriteRepository movementWriteRepository = Substitute.For<IMovementWriteRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
    private readonly Product product = CreateActiveProduct();
    private readonly RegisterAdjustmentHandler handler;

    public RegisterAdjustmentHandlerTests()
    {
        var clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(CurrentTime);
        currentUser.Subject.Returns(CurrentUserSubject);
        currentUser.AreaCodes.Returns([AreaCode]);
        productWriteRepository.GetSnapshotWithAreaForUpdateAsync(ProductId, Arg.Any<CancellationToken>()).Returns(new ProductAreaSnapshot(product, AreaCode));
        productWriteRepository.ApplyDeltaAsync(Arg.Any<InventoryMovement>(), Arg.Any<CancellationToken>()).Returns(Stock);
        movementWriteRepository.ExistsRelatedMovementAsync(Arg.Any<InventoryMovement>(), Arg.Any<CancellationToken>()).Returns(true);
        movementWriteRepository.InsertAsync(Arg.Any<InventoryMovement>(), Arg.Any<CancellationToken>())
            .Returns(call => PersistedIdentity.Assign(call.Arg<InventoryMovement>(), MovementId));
        handler = new RegisterAdjustmentHandler(productWriteRepository, movementWriteRepository, new AreaAccessPolicy(currentUser), unitOfWork, clock, currentUser);
    }

    [Fact]
    public async Task Handle_WithAdjustmentInSupervisedArea_AppliesAdjustmentDeltaToProduct()
    {
        await handler.Handle(CreateCommand(Delta, null), CancellationToken.None);
        await productWriteRepository.Received(1).ApplyDeltaAsync(
            Arg.Is<InventoryMovement>(movement => movement.ProductId == ProductId && movement.Type == MovementType.Adjustment && movement.Delta == Delta),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithAdjustmentInSupervisedArea_InsertsMovementWithReasonRelatedMovementStockCurrentTimeAndUser()
    {
        await handler.Handle(CreateCommand(Delta, RelatedMovementId), CancellationToken.None);
        await movementWriteRepository.Received(1).InsertAsync(
            Arg.Is<InventoryMovement>(movement => movement.AdjustmentReason == AdjustmentReason.Damage && movement.RelatedMovementId == RelatedMovementId
                && movement.PreviousStock == Stock && movement.ResultingStock == Stock + Delta && movement.Notes == Notes
                && movement.CreatedAt == CurrentTime && movement.CreatedBy == CurrentUserSubject),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithAdjustmentInSupervisedArea_ReturnsPersistedMovementResponse()
    {
        var response = await handler.Handle(CreateCommand(Delta, RelatedMovementId), CancellationToken.None);
        response.ShouldBe(new MovementResponse(
            MovementId, ProductId, MovementType.Adjustment, Delta, Stock, Stock + Delta, AdjustmentReason.Damage, Notes, RelatedMovementId, CurrentTime, CurrentUserSubject));
    }

    [Fact]
    public async Task Handle_WithInactiveProduct_RegistersAdjustment()
    {
        product.Deactivate(new ProductDeactivation(DeactivationReason.Expired, null, CreationTime, CurrentUserSubject));
        await handler.Handle(CreateCommand(Delta, null), CancellationToken.None);
        await movementWriteRepository.Received(1).InsertAsync(Arg.Is<InventoryMovement>(movement => movement.Type == MovementType.Adjustment), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithRelatedMovement_LocksProductChecksScopeAndRelationUpdatesStockAndInsertsInsideTransaction()
    {
        await handler.Handle(CreateCommand(Delta, RelatedMovementId), CancellationToken.None);
        Received.InOrder(() =>
        {
            unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>());
            productWriteRepository.GetSnapshotWithAreaForUpdateAsync(ProductId, Arg.Any<CancellationToken>());
            _ = currentUser.AreaCodes;
            movementWriteRepository.ExistsRelatedMovementAsync(Arg.Any<InventoryMovement>(), Arg.Any<CancellationToken>());
            productWriteRepository.ApplyDeltaAsync(Arg.Any<InventoryMovement>(), Arg.Any<CancellationToken>());
            movementWriteRepository.InsertAsync(Arg.Any<InventoryMovement>(), Arg.Any<CancellationToken>());
            unitOfWork.CommitAsync(Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task Handle_WithoutRelatedMovement_DoesNotCheckRelation()
    {
        await handler.Handle(CreateCommand(Delta, null), CancellationToken.None);
        await movementWriteRepository.DidNotReceive().ExistsRelatedMovementAsync(Arg.Any<InventoryMovement>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithRelatedMovement_ChecksRelationForAdjustedProduct()
    {
        await handler.Handle(CreateCommand(Delta, RelatedMovementId), CancellationToken.None);
        await movementWriteRepository.Received(1).ExistsRelatedMovementAsync(
            Arg.Is<InventoryMovement>(movement => movement.ProductId == ProductId && movement.RelatedMovementId == RelatedMovementId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenProductDoesNotExist_ThrowsProductNotFoundWithProductId()
    {
        productWriteRepository.GetSnapshotWithAreaForUpdateAsync(ProductId, Arg.Any<CancellationToken>()).Returns((ProductAreaSnapshot?)null);
        var exception = await Should.ThrowAsync<NotFoundException>(handler.Handle(CreateCommand(Delta, null), CancellationToken.None).AsTask());
        exception.Code.ShouldBe(ErrorCode.ProductNotFound);
        exception.MessageArguments.ShouldBe([ProductId]);
    }

    [Fact]
    public async Task Handle_WhenProductAreaIsNotSupervised_ThrowsAreaAccessDeniedWithoutTouchingStock()
    {
        currentUser.AreaCodes.Returns([OtherAreaCode]);
        var exception = await Should.ThrowAsync<ForbiddenException>(handler.Handle(CreateCommand(Delta, RelatedMovementId), CancellationToken.None).AsTask());
        exception.Code.ShouldBe(ErrorCode.AreaAccessDenied);
        exception.MessageArguments.ShouldBe([AreaCode]);
        await movementWriteRepository.DidNotReceive().ExistsRelatedMovementAsync(Arg.Any<InventoryMovement>(), Arg.Any<CancellationToken>());
        await productWriteRepository.DidNotReceive().ApplyDeltaAsync(Arg.Any<InventoryMovement>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenRelatedMovementDoesNotBelongToProduct_ThrowsRelatedMovementInvalidWithoutTouchingStock()
    {
        movementWriteRepository.ExistsRelatedMovementAsync(Arg.Any<InventoryMovement>(), Arg.Any<CancellationToken>()).Returns(false);
        var exception = await Should.ThrowAsync<InvalidReferenceException>(handler.Handle(CreateCommand(Delta, RelatedMovementId), CancellationToken.None).AsTask());
        exception.Code.ShouldBe(ErrorCode.RelatedMovementInvalid);
        exception.MessageArguments.ShouldBe([RelatedMovementId]);
        await productWriteRepository.DidNotReceive().ApplyDeltaAsync(Arg.Any<InventoryMovement>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNegativeDeltaExceedsStock_ThrowsInsufficientStockWithoutTouchingStock()
    {
        var exception = await Should.ThrowAsync<DomainException>(handler.Handle(CreateCommand(-(Stock + 1), null), CancellationToken.None).AsTask());
        exception.Code.ShouldBe(ErrorCode.InsufficientStock);
        exception.MessageArguments.ShouldBe([Stock, Stock + 1]);
        await productWriteRepository.DidNotReceive().ApplyDeltaAsync(Arg.Any<InventoryMovement>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenConditionalUpdateAffectsNoRows_ThrowsInsufficientStockWithoutInsertingOrCommitting()
    {
        productWriteRepository.ApplyDeltaAsync(Arg.Any<InventoryMovement>(), Arg.Any<CancellationToken>()).Returns((int?)null);
        var exception = await Should.ThrowAsync<DomainException>(handler.Handle(CreateCommand(Delta, null), CancellationToken.None).AsTask());
        exception.Code.ShouldBe(ErrorCode.InsufficientStock);
        exception.MessageArguments.ShouldBe([Stock, Math.Abs(Delta)]);
        await movementWriteRepository.DidNotReceive().InsertAsync(Arg.Any<InventoryMovement>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    private static RegisterAdjustmentCommand CreateCommand(int delta, long? relatedMovementId)
    {
        return new RegisterAdjustmentCommand(ProductId, new RegisterAdjustmentRequest(delta, AdjustmentReason.Damage, Notes, relatedMovementId));
    }

    private static Product CreateActiveProduct()
    {
        var area = Area.Create(AreaCode, "Food", CreationTime);
        var category = PersistedIdentity.Assign(Category.Create(area, "Dairy", CreationTime), CategoryId);
        var product = PersistedIdentity.Assign(Product.Create(category, "MILK-001", new ProductDetails("Whole milk", null, 4), CreationTime), ProductId);
        typeof(Product).GetProperty(nameof(Product.Stock))!.SetValue(product, Stock);

        return product;
    }
}
