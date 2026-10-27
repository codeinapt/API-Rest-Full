using Inventory.Application.Areas.Ports;
using Inventory.Application.Common.Interfaces;
using Mediator;

namespace Inventory.Application.Areas.Commands.DeactivateArea;

public sealed class DeactivateAreaHandler(IAreaWriteRepository areaWriteRepository, IUnitOfWork unitOfWork) : IRequestHandler<DeactivateAreaCommand>
{
    public async ValueTask<Unit> Handle(DeactivateAreaCommand request, CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        var area = await areaWriteRepository.GetExistingByIdForUpdateAsync(request.AreaId, cancellationToken);
        var activeCategoryCount = await areaWriteRepository.CountActiveCategoriesAsync(request.AreaId, cancellationToken);
        area.Deactivate(activeCategoryCount);
        await areaWriteRepository.UpdateAsync(area, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return Unit.Value;
    }
}
