using Mediator;

namespace Inventory.Application.Areas.Commands.DeactivateArea;

public sealed record DeactivateAreaCommand(int AreaId) : IRequest;
