using Inventory.Application.Products.Dtos;
using Mediator;

namespace Inventory.Application.Products.Commands.DeactivateProduct;

public sealed record DeactivateProductCommand(int ProductId, DeactivateProductRequest Request) : IRequest;
