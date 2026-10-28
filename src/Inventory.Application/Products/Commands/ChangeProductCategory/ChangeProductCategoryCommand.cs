using Inventory.Application.Products.Dtos;
using Mediator;

namespace Inventory.Application.Products.Commands.ChangeProductCategory;

public sealed record ChangeProductCategoryCommand(int ProductId, ChangeProductCategoryRequest Request) : IRequest;
