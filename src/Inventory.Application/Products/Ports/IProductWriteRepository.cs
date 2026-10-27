using Inventory.Domain.Products;

namespace Inventory.Application.Products.Ports;

public interface IProductWriteRepository
{
    Task<Product?> GetByIdForUpdateAsync(int productId, CancellationToken cancellationToken);

    Task<bool> ExistsOtherActiveProductWithSkuAsync(Product product, CancellationToken cancellationToken);

    Task<Product> InsertAsync(Product product, CancellationToken cancellationToken);

    Task UpdateDetailsAsync(Product product, CancellationToken cancellationToken);
}
