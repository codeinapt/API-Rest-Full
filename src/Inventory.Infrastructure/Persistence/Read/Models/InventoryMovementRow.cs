namespace Inventory.Infrastructure.Persistence.Read.Models;

internal sealed class InventoryMovementRow
{
    public long Id { get; init; }

    public int ProductId { get; init; }

    public byte Type { get; init; }

    public int Delta { get; init; }

    public int PreviousStock { get; init; }

    public int ResultingStock { get; init; }

    public byte? AdjustmentReason { get; init; }

    public string? Notes { get; init; }

    public long? RelatedMovementId { get; init; }

    public DateTime CreatedAt { get; init; }

    public required string CreatedBy { get; init; }
}
