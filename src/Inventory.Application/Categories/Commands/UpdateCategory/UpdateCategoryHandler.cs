using Inventory.Application.Areas.Ports;
using Inventory.Application.Categories.Ports;
using Inventory.Application.Common.Interfaces;
using Mediator;

namespace Inventory.Application.Categories.Commands.UpdateCategory;

public sealed class UpdateCategoryHandler(ICategoryWriteRepository categoryWriteRepository, IAreaWriteRepository areaWriteRepository, IUnitOfWork unitOfWork, IClock clock) : IRequestHandler<UpdateCategoryCommand>
{
    public async ValueTask<Unit> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var categoryRequest = request.Request;
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        var category = await categoryWriteRepository.GetExistingByIdForUpdateAsync(request.CategoryId, cancellationToken);
        var area = await areaWriteRepository.GetReferencedAreaForUpdateAsync(categoryRequest.AreaId, cancellationToken);
        category.Update(area, categoryRequest.Name, clock.UtcNow);
        await categoryWriteRepository.EnsureNameIsAvailableAsync(category, cancellationToken);
        await categoryWriteRepository.UpdateAsync(category, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return Unit.Value;
    }
}
