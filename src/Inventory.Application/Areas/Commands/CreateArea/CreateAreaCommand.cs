using Inventory.Application.Areas.Dtos;
using Mediator;

namespace Inventory.Application.Areas.Commands.CreateArea;

public sealed record CreateAreaCommand(CreateAreaRequest Request) : IRequest<AreaResponse>;
