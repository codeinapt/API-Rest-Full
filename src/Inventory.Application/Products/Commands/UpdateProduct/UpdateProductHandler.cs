using Inventory.Application.Common.Interfaces;
using Inventory.Application.Products.Dtos;
using Inventory.Application.Products.Ports;
using Mediator;

namespace Inventory.Application.Products.Commands.UpdateProduct;

public sealed class UpdateProductHandler(IProductWriteRepository productWriteRepository, IUnitOfWork unitOfWork, IClock clock) : IRequestHandler<UpdateProductCommand>
{
    public async ValueTask<Unit> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        var product = await productWriteRepository.GetExistingByIdForUpdateAsync(request.ProductId, cancellationToken);
        product.UpdateDetails(request.Request.ToDetails(), clock.UtcNow);
        await productWriteRepository.UpdateDetailsAsync(product, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return Unit.Value;
    }
}
