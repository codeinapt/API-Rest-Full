using Inventory.Application.Common.Exceptions;
using Inventory.Application.Products.Dtos;
using Inventory.Application.Products.Ports;
using Inventory.Domain.Common;
using Mediator;

namespace Inventory.Application.Products.Queries.GetProductById;

public sealed class GetProductByIdHandler(IProductReadRepository productReadRepository) : IRequestHandler<GetProductByIdQuery, ProductResponse>
{
    public async ValueTask<ProductResponse> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await productReadRepository.GetByIdAsync(request.ProductId, cancellationToken);

        return product ?? throw new NotFoundException(ErrorCode.ProductNotFound, request.ProductId);
    }
}
