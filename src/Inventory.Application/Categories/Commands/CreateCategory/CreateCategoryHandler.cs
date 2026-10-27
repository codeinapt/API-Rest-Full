using Inventory.Application.Areas.Ports;
using Inventory.Application.Categories.Dtos;
using Inventory.Application.Categories.Ports;
using Inventory.Application.Common.Interfaces;
using Inventory.Domain.Categories;
using Mediator;

namespace Inventory.Application.Categories.Commands.CreateCategory;

public sealed class CreateCategoryHandler(ICategoryWriteRepository categoryWriteRepository, IAreaWriteRepository areaWriteRepository, IUnitOfWork unitOfWork, IClock clock) : IRequestHandler<CreateCategoryCommand, CategoryResponse>
{
    public async ValueTask<CategoryResponse> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var categoryRequest = request.Request;
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        var area = await areaWriteRepository.GetReferencedAreaForUpdateAsync(categoryRequest.AreaId, cancellationToken);
        var category = Category.Create(area, categoryRequest.Name, clock.UtcNow);
        await categoryWriteRepository.EnsureNameIsAvailableAsync(category, cancellationToken);
        var persistedCategory = await categoryWriteRepository.InsertAsync(category, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return persistedCategory.ToResponse(area);
    }
}
