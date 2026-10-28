using Inventory.Application.Areas.Dtos;
using Inventory.Application.Areas.Queries.GetAreas;
using Inventory.Application.Common.Pagination;

namespace Inventory.Application.Areas.Ports;

public interface IAreaReadRepository
{
    Task<AreaResponse?> GetByIdAsync(int areaId, CancellationToken cancellationToken);

    Task<PagedResponse<AreaResponse>> GetPageAsync(AreaQueryParameters parameters, CancellationToken cancellationToken);
}
