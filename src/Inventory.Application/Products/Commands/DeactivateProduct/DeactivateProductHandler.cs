using Inventory.Application.Common.Interfaces;
using Inventory.Application.Products.Dtos;
using Inventory.Application.Products.Ports;
using Inventory.Domain.Common;
using Inventory.Domain.Products;
using Mediator;

namespace Inventory.Application.Products.Commands.DeactivateProduct;

public sealed class DeactivateProductHandler(IProductWriteRepository productWriteRepository, IUnitOfWork unitOfWork, IClock clock, ICurrentUser currentUser) : IRequestHandler<DeactivateProductCommand>
{
    public async ValueTask<Unit> Handle(DeactivateProductCommand request, CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        var product = await productWriteRepository.GetExistingByIdForUpdateAsync(request.ProductId, cancellationToken);
        product.Deactivate(request.Request.ToDeactivation(clock.UtcNow, currentUser.Subject));
        await EnsureDeactivationSavedAsync(product, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return Unit.Value;
    }

    private async Task EnsureDeactivationSavedAsync(Product product, CancellationToken cancellationToken)
    {
        if (!await productWriteRepository.SaveDeactivationAsync(product, cancellationToken))
        {
            throw new DomainException(ErrorCode.ProductInactive, product.Id);
        }
    }
}
