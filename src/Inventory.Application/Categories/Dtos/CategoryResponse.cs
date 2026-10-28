namespace Inventory.Application.Categories.Dtos;

public sealed record CategoryResponse(int Id, int AreaId, string AreaCode, string Name, bool IsActive);
