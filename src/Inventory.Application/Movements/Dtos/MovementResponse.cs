using Inventory.Domain.Movements;

namespace Inventory.Application.Movements.Dtos;

public sealed record MovementResponse(
    long Id,
    int ProductId,
    MovementType Type,
    int Delta,
    int PreviousStock,
    int ResultingStock,
    AdjustmentReason? AdjustmentReason,
    string? Notes,
    long? RelatedMovementId,
    DateTime CreatedAt,
    string CreatedBy);
