using Inventory.Application.Categories.Ports;
using Inventory.Application.Common.Exceptions;
using Inventory.Domain.Categories;
using Inventory.Domain.Common;

namespace Inventory.Application.Categories.Commands;

internal static class CategoryWriteRepositoryExtensions
{
    public static async Task<Category> GetExistingByIdForUpdateAsync(this ICategoryWriteRepository categoryWriteRepository, int categoryId, CancellationToken cancellationToken)
    {
        var category = await categoryWriteRepository.GetByIdForUpdateAsync(categoryId, cancellationToken);

        return category ?? throw new NotFoundException(ErrorCode.CategoryNotFound, categoryId);
    }

    public static async Task EnsureNameIsAvailableAsync(this ICategoryWriteRepository categoryWriteRepository, Category category, CancellationToken cancellationToken)
    {
        if (await categoryWriteRepository.ExistsOtherActiveCategoryWithNameAsync(category, cancellationToken))
        {
            throw new DomainException(ErrorCode.CategoryNameAlreadyExists, category.Name);
        }
    }
}
