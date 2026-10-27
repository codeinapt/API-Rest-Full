using Inventory.Application.Areas.Dtos;
using Mediator;

namespace Inventory.Application.Areas.Commands.UpdateArea;

public sealed record UpdateAreaCommand(int AreaId, UpdateAreaRequest Request) : IRequest;
