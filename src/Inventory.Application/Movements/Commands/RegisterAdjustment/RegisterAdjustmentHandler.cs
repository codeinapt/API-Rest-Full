using Inventory.Application.Common.Authorization;
using Inventory.Application.Common.Exceptions;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Movements.Dtos;
using Inventory.Application.Movements.Ports;
using Inventory.Application.Products.Commands;
using Inventory.Application.Products.Ports;
using Inventory.Domain.Common;
using Inventory.Domain.Movements;
using Inventory.Domain.Products;
using Mediator;

namespace Inventory.Application.Movements.Commands.RegisterAdjustment;

public sealed class RegisterAdjustmentHandler(IProductWriteRepository productWriteRepository, IMovementWriteRepository movementWriteRepository, AreaAccessPolicy areaAccessPolicy, IUnitOfWork unitOfWork, IClock clock, ICurrentUser currentUser) : IRequestHandler<RegisterAdjustmentCommand, MovementResponse>
{
    public async ValueTask<MovementResponse> Handle(RegisterAdjustmentCommand request, CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        var snapshot = await productWriteRepository.GetExistingSnapshotWithAreaForUpdateAsync(request.ProductId, cancellationToken);
        areaAccessPolicy.EnsureCanSupervise(snapshot.AreaCode);
        var movement = InventoryMovement.CreateAdjustment(snapshot.Product, request.Request.ToEntry(clock.UtcNow, currentUser.Subject));
        await EnsureRelatedMovementBelongsToProductAsync(movement, cancellationToken);
        await ApplyStockChangeAsync(snapshot.Product, movement, cancellationToken);
        var persistedMovement = await movementWriteRepository.InsertAsync(movement, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return persistedMovement.ToResponse();
    }

    private async Task EnsureRelatedMovementBelongsToProductAsync(InventoryMovement movement, CancellationToken cancellationToken)
    {
        if (movement.RelatedMovementId is { } relatedMovementId && !await movementWriteRepository.ExistsRelatedMovementAsync(movement, cancellationToken))
        {
            throw new InvalidReferenceException(ErrorCode.RelatedMovementInvalid, relatedMovementId);
        }
    }

    private async Task ApplyStockChangeAsync(Product product, InventoryMovement movement, CancellationToken cancellationToken)
    {
        var previousStock = await productWriteRepository.ApplyDeltaAsync(movement, cancellationToken)
            ?? throw new DomainException(ErrorCode.InsufficientStock, product.Stock, Math.Abs(movement.Delta));
        movement.RecordStockChange(previousStock);
    }
}
