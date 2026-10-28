using Inventory.Application.Common.Pagination;
using Inventory.Application.Products.Dtos;
using Mediator;

namespace Inventory.Application.Products.Queries.GetProducts;

public sealed record GetProductsQuery(ProductQueryParameters Parameters) : IRequest<PagedResponse<ProductResponse>>;
