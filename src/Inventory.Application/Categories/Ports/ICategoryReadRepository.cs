using Inventory.Application.Categories.Dtos;
using Inventory.Application.Categories.Queries.GetCategories;
using Inventory.Application.Common.Pagination;

namespace Inventory.Application.Categories.Ports;

public interface ICategoryReadRepository
{
    Task<CategoryResponse?> GetByIdAsync(int categoryId, CancellationToken cancellationToken);

    Task<PagedResponse<CategoryResponse>> GetPageAsync(CategoryQueryParameters parameters, CancellationToken cancellationToken);
}
