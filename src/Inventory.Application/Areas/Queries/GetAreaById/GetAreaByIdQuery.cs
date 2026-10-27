using Inventory.Application.Areas.Dtos;
using Mediator;

namespace Inventory.Application.Areas.Queries.GetAreaById;

public sealed record GetAreaByIdQuery(int AreaId) : IRequest<AreaResponse>;
