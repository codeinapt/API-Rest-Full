using Inventory.Application.Products.Dtos;
using Mediator;

namespace Inventory.Application.Products.Commands.CreateProduct;

public sealed record CreateProductCommand(CreateProductRequest Request) : IRequest<ProductResponse>;
