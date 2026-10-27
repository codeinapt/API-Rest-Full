using Inventory.Domain.Areas;

namespace Inventory.Application.Areas.Ports;

public interface IAreaWriteRepository
{
    Task<Area?> GetByIdForUpdateAsync(int areaId, CancellationToken cancellationToken);

    Task<bool> ExistsActiveCodeAsync(string code, CancellationToken cancellationToken);

    Task<int> CountActiveCategoriesAsync(int areaId, CancellationToken cancellationToken);

    Task<Area> InsertAsync(Area area, CancellationToken cancellationToken);

    Task UpdateAsync(Area area, CancellationToken cancellationToken);
}
