using Inventory.Application.Categories.Ports;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Products.Ports;
using Mediator;

namespace Inventory.Application.Products.Commands.ChangeProductCategory;

public sealed class ChangeProductCategoryHandler(IProductWriteRepository productWriteRepository, ICategoryWriteRepository categoryWriteRepository, IUnitOfWork unitOfWork, IClock clock) : IRequestHandler<ChangeProductCategoryCommand>
{
    public async ValueTask<Unit> Handle(ChangeProductCategoryCommand request, CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        var product = await productWriteRepository.GetExistingByIdForUpdateAsync(request.ProductId, cancellationToken);
        var category = await categoryWriteRepository.GetReferencedCategoryForUpdateAsync(request.Request.CategoryId, cancellationToken);
        product.ChangeCategory(category, clock.UtcNow);
        await productWriteRepository.UpdateCategoryAsync(product, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return Unit.Value;
    }
}
