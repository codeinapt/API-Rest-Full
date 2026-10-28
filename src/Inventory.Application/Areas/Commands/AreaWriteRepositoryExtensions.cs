using Inventory.Application.Areas.Ports;
using Inventory.Application.Common.Exceptions;
using Inventory.Domain.Areas;
using Inventory.Domain.Common;

namespace Inventory.Application.Areas.Commands;

internal static class AreaWriteRepositoryExtensions
{
    public static async Task<Area> GetExistingByIdForUpdateAsync(this IAreaWriteRepository areaWriteRepository, int areaId, CancellationToken cancellationToken)
    {
        var area = await areaWriteRepository.GetByIdForUpdateAsync(areaId, cancellationToken);

        return area ?? throw new NotFoundException(ErrorCode.AreaNotFound, areaId);
    }
}
