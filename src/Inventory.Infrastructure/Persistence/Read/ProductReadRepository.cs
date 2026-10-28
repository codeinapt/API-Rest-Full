using System.Linq.Expressions;
using Inventory.Application.Common.Pagination;
using Inventory.Application.Products.Dtos;
using Inventory.Application.Products.Ports;
using Inventory.Application.Products.Queries.GetProducts;
using Inventory.Domain.Products;
using Inventory.Infrastructure.Persistence.Read.Models;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence.Read;

internal sealed class ProductReadRepository(InventoryReadDbContext context) : IProductReadRepository
{
    public async Task<ProductResponse?> GetByIdAsync(int productId, CancellationToken cancellationToken)
    {
        var products = context.Products.Where(product => product.Id == productId);

        return await ProjectToResponse(products).SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<PagedResponse<ProductResponse>> GetPageAsync(ProductQueryParameters parameters, CancellationToken cancellationToken)
    {
        var filteredProducts = Filter(context.Products, parameters);
        var totalCount = await filteredProducts.CountAsync(cancellationToken);
        var pageOfProducts = Sort(filteredProducts, parameters)
            .Skip(parameters.CalculateItemsToSkip())
            .Take(parameters.PageSize);
        var items = await ProjectToResponse(pageOfProducts).ToListAsync(cancellationToken);

        return new PagedResponse<ProductResponse>(items, parameters, totalCount);
    }

    private static IQueryable<ProductRow> Filter(IQueryable<ProductRow> products, ProductQueryParameters parameters)
    {
        var productsOfCategory = FilterByCategory(products, parameters.CategoryId);
        var productsWithStatus = FilterByStatus(productsOfCategory, parameters.Status);
        var matchingProducts = FilterBySearch(productsWithStatus, parameters.Search);

        return FilterByLowStock(matchingProducts, parameters.LowStock);
    }

    private static IQueryable<ProductRow> FilterByCategory(IQueryable<ProductRow> products, int? categoryId)
    {
        return categoryId is null ? products : products.Where(product => product.CategoryId == categoryId);
    }

    private static IQueryable<ProductRow> FilterByStatus(IQueryable<ProductRow> products, StatusFilter status)
    {
        return status switch
        {
            StatusFilter.Active => products.Where(product => product.IsActive),
            StatusFilter.Inactive => products.Where(product => !product.IsActive),
            _ => products,
        };
    }

    private static IQueryable<ProductRow> FilterBySearch(IQueryable<ProductRow> products, string? search)
    {
        return string.IsNullOrWhiteSpace(search) ? products : products.Where(product => product.Name.Contains(search) || product.Sku.Contains(search));
    }

    private static IQueryable<ProductRow> FilterByLowStock(IQueryable<ProductRow> products, bool? lowStock)
    {
        return lowStock == true ? products.Where(product => product.Stock <= product.MinimumStock) : products;
    }

    private static IOrderedQueryable<ProductRow> Sort(IQueryable<ProductRow> products, ProductQueryParameters parameters)
    {
        var sortedProducts = parameters.SortBy switch
        {
            ProductSortField.Sku => OrderBy(products, product => product.Sku, parameters.SortDirection),
            ProductSortField.Stock => OrderBy(products, product => product.Stock, parameters.SortDirection),
            _ => OrderBy(products, product => product.Name, parameters.SortDirection),
        };

        return sortedProducts.ThenBy(product => product.Id);
    }

    private static IOrderedQueryable<ProductRow> OrderBy<TSortKey>(IQueryable<ProductRow> products, Expression<Func<ProductRow, TSortKey>> sortKey, ProductSortDirection direction)
    {
        return direction == ProductSortDirection.Desc ? products.OrderByDescending(sortKey) : products.OrderBy(sortKey);
    }

    private IQueryable<ProductResponse> ProjectToResponse(IQueryable<ProductRow> products)
    {
        return from product in products
               join category in context.Categories on product.CategoryId equals category.Id
               join area in context.Areas on category.AreaId equals area.Id
               select new ProductResponse(
                   product.Id,
                   product.Sku,
                   product.Name,
                   product.Description,
                   product.CategoryId,
                   category.Name,
                   area.Code,
                   product.Stock,
                   product.MinimumStock,
                   product.Stock <= product.MinimumStock,
                   product.IsActive,
                   product.DeactivationReason == null ? null : new ProductDeactivationResponse((DeactivationReason)product.DeactivationReason.Value, product.DeactivationNotes, product.DeactivatedAt!.Value, product.DeactivatedBy!),
                   product.ReactivatedAt == null ? null : new ProductReactivationResponse(product.ReactivatedAt.Value, product.ReactivatedBy!),
                   product.CreatedAt,
                   product.UpdatedAt);
    }
}
