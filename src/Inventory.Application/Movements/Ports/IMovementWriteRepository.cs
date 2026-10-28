using Inventory.Domain.Movements;

namespace Inventory.Application.Movements.Ports;

public interface IMovementWriteRepository
{
    Task<InventoryMovement> InsertAsync(InventoryMovement movement, CancellationToken cancellationToken);
}
