namespace Inventory.Application.Products.Dtos;

public sealed record ProductResponse(
    int Id,
    string Sku,
    string Name,
    string? Description,
    int CategoryId,
    string CategoryName,
    string AreaCode,
    int Stock,
    int MinimumStock,
    bool IsLowStock,
    bool IsActive,
    ProductDeactivationResponse? Deactivation,
    ProductReactivationResponse? Reactivation,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
