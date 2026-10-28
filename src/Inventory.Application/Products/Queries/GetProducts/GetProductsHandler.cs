using Inventory.Application.Common.Pagination;
using Inventory.Application.Products.Dtos;
using Inventory.Application.Products.Ports;
using Mediator;

namespace Inventory.Application.Products.Queries.GetProducts;

public sealed class GetProductsHandler(IProductReadRepository productReadRepository) : IRequestHandler<GetProductsQuery, PagedResponse<ProductResponse>>
{
    public async ValueTask<PagedResponse<ProductResponse>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        return await productReadRepository.GetPageAsync(request.Parameters, cancellationToken);
    }
}
