using Inventory.Domain.Movements;

namespace Inventory.Application.Movements.Dtos;

public sealed record RegisterMovementRequest(MovementType Type, int Quantity, string? Notes);
