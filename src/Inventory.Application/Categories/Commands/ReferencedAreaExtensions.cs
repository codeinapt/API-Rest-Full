using Inventory.Application.Areas.Ports;
using Inventory.Application.Common.Exceptions;
using Inventory.Domain.Areas;
using Inventory.Domain.Common;

namespace Inventory.Application.Categories.Commands;

internal static class ReferencedAreaExtensions
{
    public static async Task<Area> GetReferencedAreaForUpdateAsync(this IAreaWriteRepository areaWriteRepository, int areaId, CancellationToken cancellationToken)
    {
        var area = await areaWriteRepository.GetByIdForUpdateAsync(areaId, cancellationToken);

        return area ?? throw new InvalidReferenceException(ErrorCode.ReferencedAreaInvalid, areaId);
    }
}
