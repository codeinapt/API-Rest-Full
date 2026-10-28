namespace Inventory.Domain.Movements;

public sealed record AdjustmentEntry(int Delta, AdjustmentReason Reason, string? Notes, long? RelatedMovementId, DateTime CreatedAt, string CreatedBy);
