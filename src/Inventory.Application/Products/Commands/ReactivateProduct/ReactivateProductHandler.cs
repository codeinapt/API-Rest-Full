using Inventory.Application.Categories.Commands;
using Inventory.Application.Categories.Ports;
using Inventory.Application.Common.Authorization;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Products.Ports;
using Inventory.Domain.Common;
using Inventory.Domain.Products;
using Mediator;

namespace Inventory.Application.Products.Commands.ReactivateProduct;

public sealed class ReactivateProductHandler(IProductWriteRepository productWriteRepository, ICategoryWriteRepository categoryWriteRepository, AreaAccessPolicy areaAccessPolicy, IUnitOfWork unitOfWork, IClock clock, ICurrentUser currentUser) : IRequestHandler<ReactivateProductCommand>
{
    public async ValueTask<Unit> Handle(ReactivateProductCommand request, CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        var snapshot = await productWriteRepository.GetExistingSnapshotWithAreaForUpdateAsync(request.ProductId, cancellationToken);
        areaAccessPolicy.EnsureCanSupervise(snapshot.AreaCode);
        var product = snapshot.Product;
        var category = await categoryWriteRepository.GetExistingByIdForUpdateAsync(product.CategoryId, cancellationToken);
        product.Reactivate(category, clock.UtcNow, currentUser.Subject);
        await productWriteRepository.EnsureSkuIsAvailableAsync(product, cancellationToken);
        await EnsureReactivationSavedAsync(product, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return Unit.Value;
    }

    private async Task EnsureReactivationSavedAsync(Product product, CancellationToken cancellationToken)
    {
        if (!await productWriteRepository.ReactivateAsync(product, cancellationToken))
        {
            throw new DomainException(ErrorCode.ProductAlreadyActive, product.Id);
        }
    }
}
