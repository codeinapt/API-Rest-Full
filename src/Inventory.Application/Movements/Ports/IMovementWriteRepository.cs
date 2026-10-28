using Inventory.Domain.Movements;

namespace Inventory.Application.Movements.Ports;

public interface IMovementWriteRepository
{
    Task<bool> ExistsRelatedMovementAsync(InventoryMovement adjustment, CancellationToken cancellationToken);

    Task<InventoryMovement> InsertAsync(InventoryMovement movement, CancellationToken cancellationToken);
}
