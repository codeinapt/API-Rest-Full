using Inventory.Application.Categories.Dtos;
using Mediator;

namespace Inventory.Application.Categories.Queries.GetCategoryById;

public sealed record GetCategoryByIdQuery(int CategoryId) : IRequest<CategoryResponse>;
