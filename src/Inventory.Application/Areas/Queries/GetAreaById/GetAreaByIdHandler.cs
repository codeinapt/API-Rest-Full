using Inventory.Application.Areas.Dtos;
using Inventory.Application.Areas.Ports;
using Inventory.Application.Common.Exceptions;
using Inventory.Domain.Common;
using Mediator;

namespace Inventory.Application.Areas.Queries.GetAreaById;

public sealed class GetAreaByIdHandler(IAreaReadRepository areaReadRepository) : IRequestHandler<GetAreaByIdQuery, AreaResponse>
{
    public async ValueTask<AreaResponse> Handle(GetAreaByIdQuery request, CancellationToken cancellationToken)
    {
        var area = await areaReadRepository.GetByIdAsync(request.AreaId, cancellationToken);

        return area ?? throw new NotFoundException(ErrorCode.AreaNotFound, request.AreaId);
    }
}
