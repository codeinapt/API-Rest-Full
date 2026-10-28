using Inventory.Application.Common.Exceptions;
using Inventory.Application.Common.Pagination;
using Inventory.Application.Movements.Dtos;
using Inventory.Application.Movements.Ports;
using Inventory.Application.Products.Ports;
using Inventory.Domain.Common;
using Mediator;

namespace Inventory.Application.Movements.Queries.GetMovements;

public sealed class GetMovementsHandler(IProductReadRepository productReadRepository, IMovementReadRepository movementReadRepository) : IRequestHandler<GetMovementsQuery, PagedResponse<MovementResponse>>
{
    public async ValueTask<PagedResponse<MovementResponse>> Handle(GetMovementsQuery request, CancellationToken cancellationToken)
    {
        await EnsureProductExistsAsync(request.ProductId, cancellationToken);

        return await movementReadRepository.GetPageAsync(request, cancellationToken);
    }

    private async Task EnsureProductExistsAsync(int productId, CancellationToken cancellationToken)
    {
        var product = await productReadRepository.GetByIdAsync(productId, cancellationToken);
        if (product is null)
        {
            throw new NotFoundException(ErrorCode.ProductNotFound, productId);
        }
    }
}
