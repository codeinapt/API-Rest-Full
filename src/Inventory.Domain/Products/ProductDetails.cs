namespace Inventory.Domain.Products;

public sealed record ProductDetails(string Name, string? Description, int MinimumStock);
