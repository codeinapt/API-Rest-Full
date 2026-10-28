using Mediator;

namespace Inventory.Application.Products.Commands.ReactivateProduct;

public sealed record ReactivateProductCommand(int ProductId) : IRequest;
