using Inventory.Application.Areas.Dtos;
using Inventory.Application.Areas.Ports;
using Inventory.Application.Common.Interfaces;
using Inventory.Domain.Areas;
using Inventory.Domain.Common;
using Mediator;

namespace Inventory.Application.Areas.Commands.CreateArea;

public sealed class CreateAreaHandler(IAreaWriteRepository areaWriteRepository, IUnitOfWork unitOfWork, IClock clock) : IRequestHandler<CreateAreaCommand, AreaResponse>
{
    public async ValueTask<AreaResponse> Handle(CreateAreaCommand request, CancellationToken cancellationToken)
    {
        var areaRequest = request.Request;
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        await EnsureCodeIsAvailableAsync(areaRequest.Code, cancellationToken);
        var area = Area.Create(areaRequest.Code, areaRequest.Name, clock.UtcNow);
        var persistedArea = await areaWriteRepository.InsertAsync(area, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return persistedArea.ToResponse();
    }

    private async Task EnsureCodeIsAvailableAsync(string code, CancellationToken cancellationToken)
    {
        if (await areaWriteRepository.ExistsActiveCodeAsync(code, cancellationToken))
        {
            throw new DomainException(ErrorCode.AreaCodeAlreadyExists, code);
        }
    }
}
