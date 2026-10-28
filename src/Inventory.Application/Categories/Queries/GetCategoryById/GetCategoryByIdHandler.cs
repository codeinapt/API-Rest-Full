using Inventory.Application.Categories.Dtos;
using Inventory.Application.Categories.Ports;
using Inventory.Application.Common.Exceptions;
using Inventory.Domain.Common;
using Mediator;

namespace Inventory.Application.Categories.Queries.GetCategoryById;

public sealed class GetCategoryByIdHandler(ICategoryReadRepository categoryReadRepository) : IRequestHandler<GetCategoryByIdQuery, CategoryResponse>
{
    public async ValueTask<CategoryResponse> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        var category = await categoryReadRepository.GetByIdAsync(request.CategoryId, cancellationToken);

        return category ?? throw new NotFoundException(ErrorCode.CategoryNotFound, request.CategoryId);
    }
}
