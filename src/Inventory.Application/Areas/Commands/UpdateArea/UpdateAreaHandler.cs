using Inventory.Application.Areas.Ports;
using Inventory.Application.Common.Interfaces;
using Mediator;

namespace Inventory.Application.Areas.Commands.UpdateArea;

public sealed class UpdateAreaHandler(IAreaWriteRepository areaWriteRepository, IUnitOfWork unitOfWork, IClock clock) : IRequestHandler<UpdateAreaCommand>
{
    public async ValueTask<Unit> Handle(UpdateAreaCommand request, CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        var area = await areaWriteRepository.GetExistingByIdForUpdateAsync(request.AreaId, cancellationToken);
        area.Rename(request.Request.Name, clock.UtcNow);
        await areaWriteRepository.UpdateAsync(area, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return Unit.Value;
    }
}
