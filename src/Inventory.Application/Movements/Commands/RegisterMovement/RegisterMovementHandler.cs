using Inventory.Application.Common.Exceptions;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Movements.Dtos;
using Inventory.Application.Movements.Ports;
using Inventory.Application.Products.Ports;
using Inventory.Domain.Common;
using Inventory.Domain.Movements;
using Inventory.Domain.Products;
using Mediator;

namespace Inventory.Application.Movements.Commands.RegisterMovement;

public sealed class RegisterMovementHandler(IProductWriteRepository productWriteRepository, IMovementWriteRepository movementWriteRepository, IUnitOfWork unitOfWork, IClock clock, ICurrentUser currentUser) : IRequestHandler<RegisterMovementCommand, MovementResponse>
{
    public async ValueTask<MovementResponse> Handle(RegisterMovementCommand request, CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        var product = await GetExistingStockSnapshotAsync(request.ProductId, cancellationToken);
        var movement = CreateMovement(product, request.Request);
        await ApplyStockChangeAsync(movement, cancellationToken);
        var persistedMovement = await movementWriteRepository.InsertAsync(movement, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return persistedMovement.ToResponse();
    }

    private InventoryMovement CreateMovement(Product product, RegisterMovementRequest request)
    {
        var entry = request.ToEntry(clock.UtcNow, currentUser.Subject);

        return request.Type == MovementType.In ? InventoryMovement.CreateInbound(product, entry) : InventoryMovement.CreateOutbound(product, entry);
    }

    private async Task ApplyStockChangeAsync(InventoryMovement movement, CancellationToken cancellationToken)
    {
        var previousStock = await productWriteRepository.ApplyDeltaAsync(movement, cancellationToken)
            ?? throw await CreateConcurrentChangeExceptionAsync(movement, cancellationToken);
        movement.RecordStockChange(previousStock);
    }

    private async Task<DomainException> CreateConcurrentChangeExceptionAsync(InventoryMovement movement, CancellationToken cancellationToken)
    {
        var product = await GetExistingStockSnapshotAsync(movement.ProductId, cancellationToken);

        return product.IsActive
            ? new DomainException(ErrorCode.InsufficientStock, product.Stock, Math.Abs(movement.Delta))
            : new DomainException(ErrorCode.ProductInactive, product.Id);
    }

    private async Task<Product> GetExistingStockSnapshotAsync(int productId, CancellationToken cancellationToken)
    {
        var product = await productWriteRepository.GetStockSnapshotAsync(productId, cancellationToken);

        return product ?? throw new NotFoundException(ErrorCode.ProductNotFound, productId);
    }
}
