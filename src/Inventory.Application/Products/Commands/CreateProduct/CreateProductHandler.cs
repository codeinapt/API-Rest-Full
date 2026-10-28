using Inventory.Application.Areas.Commands;
using Inventory.Application.Areas.Ports;
using Inventory.Application.Categories.Ports;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Products.Dtos;
using Inventory.Application.Products.Ports;
using Inventory.Domain.Products;
using Mediator;

namespace Inventory.Application.Products.Commands.CreateProduct;

public sealed class CreateProductHandler(IProductWriteRepository productWriteRepository, ICategoryWriteRepository categoryWriteRepository, IAreaWriteRepository areaWriteRepository, IUnitOfWork unitOfWork, IClock clock) : IRequestHandler<CreateProductCommand, ProductResponse>
{
    public async ValueTask<ProductResponse> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var productRequest = request.Request;
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        var category = await categoryWriteRepository.GetReferencedCategoryForUpdateAsync(productRequest.CategoryId, cancellationToken);
        var product = Product.Create(category, productRequest.Sku, productRequest.ToDetails(), clock.UtcNow);
        await productWriteRepository.EnsureSkuIsAvailableAsync(product, cancellationToken);
        var area = await areaWriteRepository.GetExistingByIdForUpdateAsync(category.AreaId, cancellationToken);
        var persistedProduct = await productWriteRepository.InsertAsync(product, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return persistedProduct.ToResponse(category, area);
    }
}
