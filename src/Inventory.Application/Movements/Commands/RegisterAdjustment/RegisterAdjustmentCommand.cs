using Inventory.Application.Movements.Dtos;
using Mediator;

namespace Inventory.Application.Movements.Commands.RegisterAdjustment;

public sealed record RegisterAdjustmentCommand(int ProductId, RegisterAdjustmentRequest Request) : IRequest<MovementResponse>;
