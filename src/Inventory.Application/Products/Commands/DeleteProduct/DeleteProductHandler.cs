using Inventory.Application.Common.Interfaces;
using Inventory.Application.Products.Ports;
using Inventory.Domain.Common;
using Inventory.Domain.Products;
using Mediator;

namespace Inventory.Application.Products.Commands.DeleteProduct;

public sealed class DeleteProductHandler(IProductWriteRepository productWriteRepository, IUnitOfWork unitOfWork, IClock clock, ICurrentUser currentUser) : IRequestHandler<DeleteProductCommand>
{
    public async ValueTask<Unit> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        var product = await productWriteRepository.GetExistingByIdForUpdateAsync(request.ProductId, cancellationToken);
        product.Delete(clock.UtcNow, currentUser.Subject);
        await EnsureDeletedAsync(product, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return Unit.Value;
    }

    private async Task EnsureDeletedAsync(Product product, CancellationToken cancellationToken)
    {
        if (!await productWriteRepository.DeleteAsync(product, cancellationToken))
        {
            throw new DomainException(ErrorCode.ProductHasStock, product.Stock);
        }
    }
}
