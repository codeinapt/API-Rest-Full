using Inventory.Application.Categories.Ports;
using Inventory.Application.Common.Interfaces;
using Mediator;

namespace Inventory.Application.Categories.Commands.DeactivateCategory;

public sealed class DeactivateCategoryHandler(ICategoryWriteRepository categoryWriteRepository, IUnitOfWork unitOfWork) : IRequestHandler<DeactivateCategoryCommand>
{
    public async ValueTask<Unit> Handle(DeactivateCategoryCommand request, CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        var category = await categoryWriteRepository.GetExistingByIdForUpdateAsync(request.CategoryId, cancellationToken);
        var activeProductCount = await categoryWriteRepository.CountActiveProductsAsync(request.CategoryId, cancellationToken);
        category.Deactivate(activeProductCount);
        await categoryWriteRepository.UpdateAsync(category, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return Unit.Value;
    }
}
