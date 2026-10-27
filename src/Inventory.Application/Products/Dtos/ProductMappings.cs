using Inventory.Domain.Areas;
using Inventory.Domain.Categories;
using Inventory.Domain.Products;

namespace Inventory.Application.Products.Dtos;

public static class ProductMappings
{
    public static ProductResponse ToResponse(this Product product, Category category, Area area)
    {
        return new ProductResponse(
            product.Id,
            product.Sku,
            product.Name,
            product.Description,
            product.CategoryId,
            category.Name,
            area.Code,
            product.Stock,
            product.MinimumStock,
            product.IsLowStock(),
            product.IsActive,
            ToDeactivationResponse(product),
            ToReactivationResponse(product),
            product.CreatedAt,
            product.UpdatedAt);
    }

    public static ProductDetails ToDetails(this CreateProductRequest request)
    {
        return new ProductDetails(request.Name, request.Description, request.MinimumStock);
    }

    public static ProductDetails ToDetails(this UpdateProductRequest request)
    {
        return new ProductDetails(request.Name, request.Description, request.MinimumStock);
    }

    private static ProductDeactivationResponse? ToDeactivationResponse(Product product)
    {
        if (product.DeactivationReason is not { } reason || product.DeactivatedAt is not { } deactivatedAt || product.DeactivatedBy is not { } deactivatedBy)
        {
            return null;
        }

        return new ProductDeactivationResponse(reason, product.DeactivationNotes, deactivatedAt, deactivatedBy);
    }

    private static ProductReactivationResponse? ToReactivationResponse(Product product)
    {
        if (product.ReactivatedAt is not { } reactivatedAt || product.ReactivatedBy is not { } reactivatedBy)
        {
            return null;
        }

        return new ProductReactivationResponse(reactivatedAt, reactivatedBy);
    }
}
