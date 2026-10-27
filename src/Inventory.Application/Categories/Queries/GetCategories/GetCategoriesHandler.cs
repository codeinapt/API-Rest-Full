using Inventory.Application.Categories.Dtos;
using Inventory.Application.Categories.Ports;
using Inventory.Application.Common.Pagination;
using Mediator;

namespace Inventory.Application.Categories.Queries.GetCategories;

public sealed class GetCategoriesHandler(ICategoryReadRepository categoryReadRepository) : IRequestHandler<GetCategoriesQuery, PagedResponse<CategoryResponse>>
{
    public async ValueTask<PagedResponse<CategoryResponse>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        return await categoryReadRepository.GetPageAsync(request.Parameters, cancellationToken);
    }
}
