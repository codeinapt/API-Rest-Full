using Inventory.Application.Common.Exceptions;
using Inventory.Application.Products.Ports;
using Inventory.Domain.Common;
using Inventory.Domain.Products;

namespace Inventory.Application.Products.Commands;

internal static class ProductWriteRepositoryExtensions
{
    public static async Task<Product> GetExistingByIdForUpdateAsync(this IProductWriteRepository productWriteRepository, int productId, CancellationToken cancellationToken)
    {
        var product = await productWriteRepository.GetByIdForUpdateAsync(productId, cancellationToken);

        return product ?? throw new NotFoundException(ErrorCode.ProductNotFound, productId);
    }

    public static async Task<ProductAreaSnapshot> GetExistingSnapshotWithAreaForUpdateAsync(this IProductWriteRepository productWriteRepository, int productId, CancellationToken cancellationToken)
    {
        var snapshot = await productWriteRepository.GetSnapshotWithAreaForUpdateAsync(productId, cancellationToken);

        return snapshot ?? throw new NotFoundException(ErrorCode.ProductNotFound, productId);
    }

    public static async Task EnsureSkuIsAvailableAsync(this IProductWriteRepository productWriteRepository, Product product, CancellationToken cancellationToken)
    {
        if (await productWriteRepository.ExistsOtherActiveProductWithSkuAsync(product, cancellationToken))
        {
            throw new DomainException(ErrorCode.SkuAlreadyExists, product.Sku);
        }
    }
}
