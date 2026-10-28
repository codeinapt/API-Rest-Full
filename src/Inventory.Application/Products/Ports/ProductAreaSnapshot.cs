using Inventory.Domain.Products;

namespace Inventory.Application.Products.Ports;

public sealed record ProductAreaSnapshot(Product Product, string AreaCode);
