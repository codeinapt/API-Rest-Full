using Inventory.Application.Areas.Commands.CreateArea;
using Inventory.Application.Areas.Dtos;
using Inventory.Application.Categories.Commands.CreateCategory;
using Inventory.Application.Categories.Dtos;
using Inventory.Application.Movements.Commands.RegisterAdjustment;
using Inventory.Application.Movements.Commands.RegisterMovement;
using Inventory.Application.Movements.Dtos;
using Inventory.Application.Products.Commands.CreateProduct;
using Inventory.Application.Products.Commands.DeactivateProduct;
using Inventory.Application.Products.Commands.DeleteProduct;
using Inventory.Application.Products.Dtos;
using Inventory.Domain.Movements;
using Inventory.Domain.Products;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.IntegrationTests.Common;

internal sealed class InventoryOperations(SqlServerFixture fixture)
{
    private const string TestAreaName = "Integration tests";
    private const string TestCategoryName = "Integration test products";
    private const string TestProductName = "Integration test product";

    public static string CreateUniqueSku()
    {
        return $"IT-{Guid.NewGuid():N}".ToUpperInvariant();
    }

    public async Task<AreaResponse> CreateAreaAsync(string code)
    {
        var command = new CreateAreaCommand(new CreateAreaRequest(code, TestAreaName));

        return await HandleAsync<CreateAreaHandler, AreaResponse>(handler => handler.Handle(command, CancellationToken.None));
    }

    public async Task<CategoryResponse> CreateCategoryAsync(int areaId)
    {
        var command = new CreateCategoryCommand(new CreateCategoryRequest(areaId, TestCategoryName));

        return await HandleAsync<CreateCategoryHandler, CategoryResponse>(handler => handler.Handle(command, CancellationToken.None));
    }

    public async Task<ProductResponse> CreateProductAsync(string sku)
    {
        var command = new CreateProductCommand(new CreateProductRequest(sku, TestProductName, null, fixture.TestCategoryId, 0));

        return await HandleAsync<CreateProductHandler, ProductResponse>(handler => handler.Handle(command, CancellationToken.None));
    }

    public async Task<int> CreateProductWithStockAsync(int stock)
    {
        var product = await CreateProductAsync(CreateUniqueSku());
        await RegisterMovementAsync(product.Id, MovementType.In, stock);

        return product.Id;
    }

    public async Task<MovementResponse> RegisterMovementAsync(int productId, MovementType type, int quantity)
    {
        var command = new RegisterMovementCommand(productId, new RegisterMovementRequest(type, quantity, null));

        return await HandleAsync<RegisterMovementHandler, MovementResponse>(handler => handler.Handle(command, CancellationToken.None));
    }

    public async Task<Exception?[]> RegisterConcurrentOutboundsAsync(int productId, int outboundCount, int quantity)
    {
        var outbounds = Enumerable.Range(0, outboundCount)
            .Select(_ => Record.ExceptionAsync(() => RegisterMovementAsync(productId, MovementType.Out, quantity)).AsTask());

        return await Task.WhenAll(outbounds);
    }

    public async Task<MovementResponse> RegisterAdjustmentAsync(int productId, int delta)
    {
        var command = new RegisterAdjustmentCommand(productId, new RegisterAdjustmentRequest(delta, AdjustmentReason.Damage, null, null));

        return await HandleAsync<RegisterAdjustmentHandler, MovementResponse>(handler => handler.Handle(command, CancellationToken.None));
    }

    public async Task DeleteProductAsync(int productId)
    {
        await HandleAsync<DeleteProductHandler, Mediator.Unit>(handler => handler.Handle(new DeleteProductCommand(productId), CancellationToken.None));
    }

    public async Task DeactivateProductAsync(int productId)
    {
        var command = new DeactivateProductCommand(productId, new DeactivateProductRequest(DeactivationReason.Recalled, null));
        await HandleAsync<DeactivateProductHandler, Mediator.Unit>(handler => handler.Handle(command, CancellationToken.None));
    }

    private async Task<TResponse> HandleAsync<THandler, TResponse>(Func<THandler, ValueTask<TResponse>> handle)
        where THandler : notnull
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var handler = ActivatorUtilities.CreateInstance<THandler>(scope.ServiceProvider);

        return await handle(handler);
    }
}
