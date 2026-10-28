using Inventory.Domain.Areas;
using Inventory.Domain.Categories;
using Inventory.Domain.Common;
using Inventory.Domain.Movements;
using Inventory.Domain.Products;
using Inventory.Domain.Tests.Common;

namespace Inventory.Domain.Tests.Movements;

public sealed class InventoryMovementTests
{
    private const string UserId = "user-sub";
    private const string ValidNotes = "Supplier delivery";
    private const int ProductId = 100;
    private const long RelatedMovementId = 42;
    private static readonly DateTime CreationTime = new(2026, 10, 28, 9, 0, 0, DateTimeKind.Utc);

    public static TheoryData<int> InvalidQuantities()
    {
        return new TheoryData<int> { 0, -1, InventoryMovement.MaximumQuantity + 1 };
    }

    public static TheoryData<int> InvalidDeltas()
    {
        return new TheoryData<int> { 0, InventoryMovement.MaximumQuantity + 1, -InventoryMovement.MaximumQuantity - 1, int.MinValue };
    }

    public static TheoryData<AdjustmentReason> UndefinedReasons()
    {
        return new TheoryData<AdjustmentReason> { (AdjustmentReason)0, (AdjustmentReason)7 };
    }

    [Fact]
    public void CreateInbound_WithValidEntry_ReturnsInMovementWithPositiveDelta()
    {
        var movement = InventoryMovement.CreateInbound(CreateActiveProduct(10), new MovementEntry(4, ValidNotes, CreationTime, UserId));
        movement.ProductId.ShouldBe(ProductId);
        movement.Type.ShouldBe(MovementType.In);
        movement.Delta.ShouldBe(4);
        movement.PreviousStock.ShouldBe(10);
        movement.ResultingStock.ShouldBe(14);
        movement.AdjustmentReason.ShouldBeNull();
        movement.Notes.ShouldBe(ValidNotes);
        movement.RelatedMovementId.ShouldBeNull();
        movement.CreatedAt.ShouldBe(CreationTime);
        movement.CreatedBy.ShouldBe(UserId);
    }

    [Fact]
    public void CreateInbound_WithMaximumQuantityAndNotes_ReturnsMovement()
    {
        var entry = new MovementEntry(InventoryMovement.MaximumQuantity, new string('N', InventoryMovement.NotesMaximumLength), CreationTime, UserId);
        var movement = InventoryMovement.CreateInbound(CreateActiveProduct(0), entry);
        movement.Delta.ShouldBe(InventoryMovement.MaximumQuantity);
    }

    [Theory]
    [MemberData(nameof(InvalidQuantities))]
    public void CreateInbound_WithInvalidQuantity_ThrowsValidationError(int quantity)
    {
        var entry = new MovementEntry(quantity, null, CreationTime, UserId);
        var exception = Should.Throw<DomainException>(() => InventoryMovement.CreateInbound(CreateActiveProduct(10), entry));
        exception.Code.ShouldBe(ErrorCode.ValidationError);
    }

    [Fact]
    public void CreateInbound_WithTooLongNotes_ThrowsValidationError()
    {
        var entry = new MovementEntry(4, new string('N', InventoryMovement.NotesMaximumLength + 1), CreationTime, UserId);
        var exception = Should.Throw<DomainException>(() => InventoryMovement.CreateInbound(CreateActiveProduct(10), entry));
        exception.Code.ShouldBe(ErrorCode.ValidationError);
    }

    [Fact]
    public void CreateInbound_OnInactiveProduct_ThrowsProductInactiveWithId()
    {
        var entry = new MovementEntry(4, null, CreationTime, UserId);
        var exception = Should.Throw<DomainException>(() => InventoryMovement.CreateInbound(CreateInactiveProduct(10), entry));
        exception.Code.ShouldBe(ErrorCode.ProductInactive);
        exception.MessageArguments.ShouldBe([ProductId]);
    }

    [Fact]
    public void CreateInbound_OnInactiveProductWithInvalidQuantity_ThrowsValidationErrorFirst()
    {
        var entry = new MovementEntry(0, null, CreationTime, UserId);
        var exception = Should.Throw<DomainException>(() => InventoryMovement.CreateInbound(CreateInactiveProduct(10), entry));
        exception.Code.ShouldBe(ErrorCode.ValidationError);
    }

    [Fact]
    public void CreateOutbound_WithValidEntry_ReturnsOutMovementWithNegativeDelta()
    {
        var movement = InventoryMovement.CreateOutbound(CreateActiveProduct(10), new MovementEntry(4, ValidNotes, CreationTime, UserId));
        movement.ProductId.ShouldBe(ProductId);
        movement.Type.ShouldBe(MovementType.Out);
        movement.Delta.ShouldBe(-4);
        movement.PreviousStock.ShouldBe(10);
        movement.ResultingStock.ShouldBe(6);
        movement.AdjustmentReason.ShouldBeNull();
        movement.Notes.ShouldBe(ValidNotes);
        movement.RelatedMovementId.ShouldBeNull();
        movement.CreatedAt.ShouldBe(CreationTime);
        movement.CreatedBy.ShouldBe(UserId);
    }

    [Fact]
    public void CreateOutbound_WithWholeStock_LeavesZeroStock()
    {
        var movement = InventoryMovement.CreateOutbound(CreateActiveProduct(10), new MovementEntry(10, null, CreationTime, UserId));
        movement.ResultingStock.ShouldBe(0);
    }

    [Theory]
    [MemberData(nameof(InvalidQuantities))]
    public void CreateOutbound_WithInvalidQuantity_ThrowsValidationError(int quantity)
    {
        var entry = new MovementEntry(quantity, null, CreationTime, UserId);
        var exception = Should.Throw<DomainException>(() => InventoryMovement.CreateOutbound(CreateActiveProduct(10), entry));
        exception.Code.ShouldBe(ErrorCode.ValidationError);
    }

    [Fact]
    public void CreateOutbound_WithTooLongNotes_ThrowsValidationError()
    {
        var entry = new MovementEntry(4, new string('N', InventoryMovement.NotesMaximumLength + 1), CreationTime, UserId);
        var exception = Should.Throw<DomainException>(() => InventoryMovement.CreateOutbound(CreateActiveProduct(10), entry));
        exception.Code.ShouldBe(ErrorCode.ValidationError);
    }

    [Fact]
    public void CreateOutbound_WithMoreThanStock_ThrowsInsufficientStockWithStockAndQuantity()
    {
        var entry = new MovementEntry(11, null, CreationTime, UserId);
        var exception = Should.Throw<DomainException>(() => InventoryMovement.CreateOutbound(CreateActiveProduct(10), entry));
        exception.Code.ShouldBe(ErrorCode.InsufficientStock);
        exception.MessageArguments.ShouldBe([10, 11]);
    }

    [Fact]
    public void CreateOutbound_OnInactiveProduct_ThrowsProductInactiveWithId()
    {
        var entry = new MovementEntry(4, null, CreationTime, UserId);
        var exception = Should.Throw<DomainException>(() => InventoryMovement.CreateOutbound(CreateInactiveProduct(10), entry));
        exception.Code.ShouldBe(ErrorCode.ProductInactive);
        exception.MessageArguments.ShouldBe([ProductId]);
    }

    [Fact]
    public void CreateOutbound_OnInactiveProductWithMoreThanStock_ThrowsProductInactiveFirst()
    {
        var entry = new MovementEntry(11, null, CreationTime, UserId);
        var exception = Should.Throw<DomainException>(() => InventoryMovement.CreateOutbound(CreateInactiveProduct(10), entry));
        exception.Code.ShouldBe(ErrorCode.ProductInactive);
    }

    [Fact]
    public void CreateAdjustment_WithValidEntry_ReturnsAdjustmentWithReasonAndRelatedMovement()
    {
        var entry = new AdjustmentEntry(-3, AdjustmentReason.DataEntryError, ValidNotes, RelatedMovementId, CreationTime, UserId);
        var movement = InventoryMovement.CreateAdjustment(CreateActiveProduct(10), entry);
        movement.ProductId.ShouldBe(ProductId);
        movement.Type.ShouldBe(MovementType.Adjustment);
        movement.Delta.ShouldBe(-3);
        movement.PreviousStock.ShouldBe(10);
        movement.ResultingStock.ShouldBe(7);
        movement.AdjustmentReason.ShouldBe(AdjustmentReason.DataEntryError);
        movement.Notes.ShouldBe(ValidNotes);
        movement.RelatedMovementId.ShouldBe(RelatedMovementId);
        movement.CreatedAt.ShouldBe(CreationTime);
        movement.CreatedBy.ShouldBe(UserId);
    }

    [Fact]
    public void CreateAdjustment_WithPositiveDeltaAndNoRelatedMovement_IncreasesStock()
    {
        var entry = new AdjustmentEntry(InventoryMovement.MaximumQuantity, AdjustmentReason.Found, null, null, CreationTime, UserId);
        var movement = InventoryMovement.CreateAdjustment(CreateActiveProduct(10), entry);
        movement.ResultingStock.ShouldBe(10 + InventoryMovement.MaximumQuantity);
        movement.RelatedMovementId.ShouldBeNull();
    }

    [Fact]
    public void CreateAdjustment_OnInactiveProduct_ReturnsAdjustment()
    {
        var entry = new AdjustmentEntry(-10, AdjustmentReason.Expiration, null, null, CreationTime, UserId);
        var movement = InventoryMovement.CreateAdjustment(CreateInactiveProduct(10), entry);
        movement.ResultingStock.ShouldBe(0);
    }

    [Theory]
    [MemberData(nameof(InvalidDeltas))]
    public void CreateAdjustment_WithInvalidDelta_ThrowsValidationError(int delta)
    {
        var entry = new AdjustmentEntry(delta, AdjustmentReason.PhysicalCountCorrection, null, null, CreationTime, UserId);
        var exception = Should.Throw<DomainException>(() => InventoryMovement.CreateAdjustment(CreateActiveProduct(10), entry));
        exception.Code.ShouldBe(ErrorCode.ValidationError);
    }

    [Theory]
    [MemberData(nameof(UndefinedReasons))]
    public void CreateAdjustment_WithUndefinedReason_ThrowsValidationError(AdjustmentReason reason)
    {
        var entry = new AdjustmentEntry(3, reason, null, null, CreationTime, UserId);
        var exception = Should.Throw<DomainException>(() => InventoryMovement.CreateAdjustment(CreateActiveProduct(10), entry));
        exception.Code.ShouldBe(ErrorCode.ValidationError);
    }

    [Fact]
    public void CreateAdjustment_WithTooLongNotes_ThrowsValidationError()
    {
        var notes = new string('N', InventoryMovement.NotesMaximumLength + 1);
        var entry = new AdjustmentEntry(3, AdjustmentReason.Found, notes, null, CreationTime, UserId);
        var exception = Should.Throw<DomainException>(() => InventoryMovement.CreateAdjustment(CreateActiveProduct(10), entry));
        exception.Code.ShouldBe(ErrorCode.ValidationError);
    }

    [Fact]
    public void CreateAdjustment_WithNegativeDeltaAboveStock_ThrowsInsufficientStockWithStockAndAbsoluteDelta()
    {
        var entry = new AdjustmentEntry(-11, AdjustmentReason.LossOrTheft, null, null, CreationTime, UserId);
        var exception = Should.Throw<DomainException>(() => InventoryMovement.CreateAdjustment(CreateInactiveProduct(10), entry));
        exception.Code.ShouldBe(ErrorCode.InsufficientStock);
        exception.MessageArguments.ShouldBe([10, 11]);
    }

    [Fact]
    public void RecordStockChange_WithPreviousStockFromDatabase_RecalculatesResultingStock()
    {
        var movement = InventoryMovement.CreateOutbound(CreateActiveProduct(10), new MovementEntry(4, null, CreationTime, UserId));
        movement.RecordStockChange(25);
        movement.PreviousStock.ShouldBe(25);
        movement.ResultingStock.ShouldBe(21);
    }

    private static Product CreateActiveProduct(int stock)
    {
        var area = PersistedIdentity.Assign(Area.Create("FOOD", "Food", CreationTime), 1);
        var category = PersistedIdentity.Assign(Category.Create(area, "Dairy", CreationTime), 10);
        var product = PersistedIdentity.Assign(Product.Create(category, "MILK-1L", new ProductDetails("Whole milk", null, 5), CreationTime), ProductId);
        typeof(Product).GetProperty(nameof(Product.Stock))!.SetValue(product, stock);

        return product;
    }

    private static Product CreateInactiveProduct(int stock)
    {
        var product = CreateActiveProduct(stock);
        product.Deactivate(new ProductDeactivation(DeactivationReason.Expired, null, CreationTime, UserId));

        return product;
    }
}
