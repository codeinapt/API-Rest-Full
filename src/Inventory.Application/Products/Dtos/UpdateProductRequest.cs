namespace Inventory.Application.Products.Dtos;

public sealed record UpdateProductRequest(string Name, string? Description, int MinimumStock);
