using Inventory.Application.Categories.Ports;
using Inventory.Application.Common.Exceptions;
using Inventory.Domain.Categories;
using Inventory.Domain.Common;

namespace Inventory.Application.Products.Commands;

internal static class ReferencedCategoryExtensions
{
    public static async Task<Category> GetReferencedCategoryForUpdateAsync(this ICategoryWriteRepository categoryWriteRepository, int categoryId, CancellationToken cancellationToken)
    {
        var category = await categoryWriteRepository.GetByIdForUpdateAsync(categoryId, cancellationToken);

        return category ?? throw new InvalidReferenceException(ErrorCode.ReferencedCategoryInvalid, categoryId);
    }
}
