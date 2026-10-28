using Inventory.Domain.Common;
using Inventory.Domain.Products;

namespace Inventory.Domain.Movements;

public sealed class InventoryMovement
{
    public const int MaximumQuantity = 1_000_000;

    public const int NotesMaximumLength = 500;

    private InventoryMovement()
    {
    }

    public long Id { get; private set; }

    public int ProductId { get; private set; }

    public MovementType Type { get; private set; }

    public int Delta { get; private set; }

    public int PreviousStock { get; private set; }

    public int ResultingStock { get; private set; }

    public AdjustmentReason? AdjustmentReason { get; private set; }

    public string? Notes { get; private set; }

    public long? RelatedMovementId { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public string CreatedBy { get; private set; } = string.Empty;

    public static InventoryMovement CreateInbound(Product product, MovementEntry entry)
    {
        EnsureValidEntry(entry);
        EnsureActive(product);

        return FromEntry(product, MovementType.In, entry.Quantity, entry);
    }

    public static InventoryMovement CreateOutbound(Product product, MovementEntry entry)
    {
        EnsureValidEntry(entry);
        EnsureActive(product);
        EnsureStockStaysNonNegative(product, -entry.Quantity);

        return FromEntry(product, MovementType.Out, -entry.Quantity, entry);
    }

    public static InventoryMovement CreateAdjustment(Product product, AdjustmentEntry entry)
    {
        EnsureValidAdjustment(entry);
        EnsureStockStaysNonNegative(product, entry.Delta);
        var movement = new InventoryMovement
        {
            Type = MovementType.Adjustment,
            Delta = entry.Delta,
            AdjustmentReason = entry.Reason,
            Notes = entry.Notes,
            RelatedMovementId = entry.RelatedMovementId,
            CreatedAt = entry.CreatedAt,
            CreatedBy = entry.CreatedBy,
        };
        movement.TakeSnapshot(product);

        return movement;
    }

    public void RecordStockChange(int previousStock)
    {
        PreviousStock = previousStock;
        ResultingStock = previousStock + Delta;
    }

    private static InventoryMovement FromEntry(Product product, MovementType type, int delta, MovementEntry entry)
    {
        var movement = new InventoryMovement { Type = type, Delta = delta, Notes = entry.Notes, CreatedAt = entry.CreatedAt, CreatedBy = entry.CreatedBy };
        movement.TakeSnapshot(product);

        return movement;
    }

    private static void EnsureValidEntry(MovementEntry entry)
    {
        var hasValidQuantity = entry.Quantity is > 0 and <= MaximumQuantity;
        if (!hasValidQuantity || !HasValidNotesLength(entry.Notes))
        {
            throw new DomainException(ErrorCode.ValidationError);
        }
    }

    private static void EnsureValidAdjustment(AdjustmentEntry entry)
    {
        var hasValidDelta = entry.Delta is >= -MaximumQuantity and <= MaximumQuantity and not 0;
        if (!hasValidDelta || !Enum.IsDefined(entry.Reason) || !HasValidNotesLength(entry.Notes))
        {
            throw new DomainException(ErrorCode.ValidationError);
        }
    }

    private static bool HasValidNotesLength(string? notes)
    {
        return notes is null || notes.Length <= NotesMaximumLength;
    }

    private static void EnsureActive(Product product)
    {
        if (!product.IsActive)
        {
            throw new DomainException(ErrorCode.ProductInactive, product.Id);
        }
    }

    private static void EnsureStockStaysNonNegative(Product product, int delta)
    {
        if (product.Stock + delta < 0)
        {
            throw new DomainException(ErrorCode.InsufficientStock, product.Stock, -delta);
        }
    }

    private void TakeSnapshot(Product product)
    {
        ProductId = product.Id;
        RecordStockChange(product.Stock);
    }
}
