using Inventory.Application.Categories.Dtos;
using Inventory.Application.Categories.Ports;
using Inventory.Application.Categories.Queries.GetCategories;
using Inventory.Application.Common.Pagination;
using Inventory.Infrastructure.Persistence.Read.Models;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence.Read;

internal sealed class CategoryReadRepository(InventoryReadDbContext context) : ICategoryReadRepository
{
    public async Task<CategoryResponse?> GetByIdAsync(int categoryId, CancellationToken cancellationToken)
    {
        var categories = context.Categories.Where(category => category.Id == categoryId);

        return await ProjectToResponse(categories).SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<PagedResponse<CategoryResponse>> GetPageAsync(CategoryQueryParameters parameters, CancellationToken cancellationToken)
    {
        var filteredCategories = FilterByStatus(FilterByArea(context.Categories, parameters.AreaId), parameters.Status);
        var totalCount = await filteredCategories.CountAsync(cancellationToken);
        var pageOfCategories = filteredCategories
            .OrderBy(category => category.Name)
            .ThenBy(category => category.Id)
            .Skip(parameters.CalculateItemsToSkip())
            .Take(parameters.PageSize);
        var items = await ProjectToResponse(pageOfCategories).ToListAsync(cancellationToken);

        return new PagedResponse<CategoryResponse>(items, parameters, totalCount);
    }

    private static IQueryable<CategoryRow> FilterByArea(IQueryable<CategoryRow> categories, int? areaId)
    {
        return areaId is null ? categories : categories.Where(category => category.AreaId == areaId);
    }

    private static IQueryable<CategoryRow> FilterByStatus(IQueryable<CategoryRow> categories, StatusFilter status)
    {
        return status switch
        {
            StatusFilter.Active => categories.Where(category => category.IsActive),
            StatusFilter.Inactive => categories.Where(category => !category.IsActive),
            _ => categories,
        };
    }

    private IQueryable<CategoryResponse> ProjectToResponse(IQueryable<CategoryRow> categories)
    {
        return categories.Join(
            context.Areas,
            category => category.AreaId,
            area => area.Id,
            (category, area) => new CategoryResponse(category.Id, category.AreaId, area.Code, category.Name, category.IsActive));
    }
}
