using Inventory.Domain.Movements;

namespace Inventory.Application.Movements.Dtos;

public static class MovementMappings
{
    public static MovementResponse ToResponse(this InventoryMovement movement)
    {
        return new MovementResponse(
            movement.Id,
            movement.ProductId,
            movement.Type,
            movement.Delta,
            movement.PreviousStock,
            movement.ResultingStock,
            movement.AdjustmentReason,
            movement.Notes,
            movement.RelatedMovementId,
            movement.CreatedAt,
            movement.CreatedBy);
    }

    public static MovementEntry ToEntry(this RegisterMovementRequest request, DateTime createdAt, string createdBy)
    {
        return new MovementEntry(request.Quantity, request.Notes, createdAt, createdBy);
    }

    public static AdjustmentEntry ToEntry(this RegisterAdjustmentRequest request, DateTime createdAt, string createdBy)
    {
        return new AdjustmentEntry(request.Delta, request.Reason, request.Notes, request.RelatedMovementId, createdAt, createdBy);
    }
}
