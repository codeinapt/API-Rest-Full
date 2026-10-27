using Inventory.Domain.Categories;

namespace Inventory.Application.Categories.Ports;

public interface ICategoryWriteRepository
{
    Task<Category?> GetByIdForUpdateAsync(int categoryId, CancellationToken cancellationToken);

    Task<bool> ExistsOtherActiveCategoryWithNameAsync(Category category, CancellationToken cancellationToken);

    Task<int> CountActiveProductsAsync(int categoryId, CancellationToken cancellationToken);

    Task<Category> InsertAsync(Category category, CancellationToken cancellationToken);

    Task UpdateAsync(Category category, CancellationToken cancellationToken);
}
