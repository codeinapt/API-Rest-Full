using Inventory.Application.Categories.Dtos;
using Inventory.Application.Common.Pagination;
using Mediator;

namespace Inventory.Application.Categories.Queries.GetCategories;

public sealed record GetCategoriesQuery(CategoryQueryParameters Parameters) : IRequest<PagedResponse<CategoryResponse>>;
