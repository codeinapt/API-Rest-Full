using Inventory.Application.Movements.Dtos;
using Mediator;

namespace Inventory.Application.Movements.Commands.RegisterMovement;

public sealed record RegisterMovementCommand(int ProductId, RegisterMovementRequest Request) : IRequest<MovementResponse>;
