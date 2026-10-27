using Inventory.Application.Categories.Dtos;
using Mediator;

namespace Inventory.Application.Categories.Commands.CreateCategory;

public sealed record CreateCategoryCommand(CreateCategoryRequest Request) : IRequest<CategoryResponse>;
