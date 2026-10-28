using Inventory.Domain.Areas;

namespace Inventory.Application.Areas.Dtos;

public static class AreaMappings
{
    public static AreaResponse ToResponse(this Area area)
    {
        return new AreaResponse(area.Id, area.Code, area.Name, area.IsActive);
    }
}
