using Inventory.Domain.Areas;
using Inventory.Domain.Categories;

namespace Inventory.Application.Categories.Dtos;

public static class CategoryMappings
{
    public static CategoryResponse ToResponse(this Category category, Area area)
    {
        return new CategoryResponse(category.Id, category.AreaId, area.Code, category.Name, category.IsActive);
    }
}
