using Inventory.Application.Products.Dtos;
using Mediator;

namespace Inventory.Application.Products.Queries.GetProductById;

public sealed record GetProductByIdQuery(int ProductId) : IRequest<ProductResponse>;
