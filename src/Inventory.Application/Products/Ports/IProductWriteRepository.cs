using Inventory.Domain.Products;

namespace Inventory.Application.Products.Ports;

public interface IProductWriteRepository
{
    Task<Product?> GetByIdForUpdateAsync(int productId, CancellationToken cancellationToken);

    Task<ProductAreaSnapshot?> GetSnapshotWithAreaForUpdateAsync(int productId, CancellationToken cancellationToken);

    Task<bool> ExistsOtherActiveProductWithSkuAsync(Product product, CancellationToken cancellationToken);

    Task<Product> InsertAsync(Product product, CancellationToken cancellationToken);

    Task UpdateDetailsAsync(Product product, CancellationToken cancellationToken);

    Task UpdateCategoryAsync(Product product, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Product product, CancellationToken cancellationToken);

    Task<bool> SaveDeactivationAsync(Product product, CancellationToken cancellationToken);

    Task<bool> ReactivateAsync(Product product, CancellationToken cancellationToken);
}
