namespace Inventory.Application.Products.Dtos;

public sealed record CreateProductRequest(string Sku, string Name, string? Description, int CategoryId, int MinimumStock);
