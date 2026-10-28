using Inventory.Application.Areas.Dtos;
using Inventory.Application.Areas.Ports;
using Inventory.Application.Common.Pagination;
using Mediator;

namespace Inventory.Application.Areas.Queries.GetAreas;

public sealed class GetAreasHandler(IAreaReadRepository areaReadRepository) : IRequestHandler<GetAreasQuery, PagedResponse<AreaResponse>>
{
    public async ValueTask<PagedResponse<AreaResponse>> Handle(GetAreasQuery request, CancellationToken cancellationToken)
    {
        return await areaReadRepository.GetPageAsync(request.Parameters, cancellationToken);
    }
}
