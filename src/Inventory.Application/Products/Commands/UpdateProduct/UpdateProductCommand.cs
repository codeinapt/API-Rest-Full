using Inventory.Application.Products.Dtos;
using Mediator;

namespace Inventory.Application.Products.Commands.UpdateProduct;

public sealed record UpdateProductCommand(int ProductId, UpdateProductRequest Request) : IRequest;
