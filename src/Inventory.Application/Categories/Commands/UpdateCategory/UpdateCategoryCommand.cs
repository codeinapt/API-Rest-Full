using Inventory.Application.Categories.Dtos;
using Mediator;

namespace Inventory.Application.Categories.Commands.UpdateCategory;

public sealed record UpdateCategoryCommand(int CategoryId, UpdateCategoryRequest Request) : IRequest;
