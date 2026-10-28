using Inventory.Domain.Movements;

namespace Inventory.Application.Movements.Dtos;

public sealed record RegisterAdjustmentRequest(int Delta, AdjustmentReason Reason, string? Notes, long? RelatedMovementId);
