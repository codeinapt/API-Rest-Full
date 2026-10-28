using Inventory.Application.Common.Pagination;
using Inventory.Application.Products.Dtos;
using Inventory.Application.Products.Queries.GetProducts;

namespace Inventory.Application.Products.Ports;

public interface IProductReadRepository
{
    Task<ProductResponse?> GetByIdAsync(int productId, CancellationToken cancellationToken);

    Task<PagedResponse<ProductResponse>> GetPageAsync(ProductQueryParameters parameters, CancellationToken cancellationToken);
}
