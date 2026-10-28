using Inventory.Application.Areas.Dtos;
using Inventory.Application.Common.Pagination;
using Mediator;

namespace Inventory.Application.Areas.Queries.GetAreas;

public sealed record GetAreasQuery(AreaQueryParameters Parameters) : IRequest<PagedResponse<AreaResponse>>;
