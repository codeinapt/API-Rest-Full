using Inventory.Domain.Common;
using Inventory.Domain.Movements;
using Inventory.Domain.Products;
using Inventory.IntegrationTests.Common;

namespace Inventory.IntegrationTests.Products;

public sealed class DeleteProductConcurrencyTests(SqlServerFixture fixture)
{
    private const int InboundQuantity = 5;

    private readonly InventoryOperations operations = new(fixture);
    private readonly DatabaseQueries queries = new(fixture);

    [Fact]
    public async Task DeleteAsync_WhenInboundCommittedAfterSnapshot_DoesNotDeleteProduct()
    {
        var product = await operations.CreateProductAsync(InventoryOperations.CreateUniqueSku());
        var deletedSnapshot = await ReadDeletedSnapshotAsync(product.Id);
        await operations.RegisterMovementAsync(product.Id, MovementType.In, InboundQuantity);
        await using var scope = await WriteScope.BeginAsync(fixture);
        (await scope.Products.DeleteAsync(deletedSnapshot, CancellationToken.None)).ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteProduct_WhenInboundIsQueuedFirst_FailsWithProductHasStock()
    {
        var product = await operations.CreateProductAsync(InventoryOperations.CreateUniqueSku());
        var (inboundOutcome, deleteOutcome) = await RunQueuedAsync(product.Id, () => RegisterInboundAsync(product.Id), () => operations.DeleteProductAsync(product.Id));
        deleteOutcome.ShouldSatisfyAllConditions(
            () => inboundOutcome.ShouldBeNull(),
            () => deleteOutcome.ShouldBeOfType<DomainException>().Code.ShouldBe(ErrorCode.ProductHasStock));
    }

    [Fact]
    public async Task DeleteProduct_WhenInboundIsQueuedFirst_KeepsProductActiveWithInboundStock()
    {
        var product = await operations.CreateProductAsync(InventoryOperations.CreateUniqueSku());
        await RunQueuedAsync(product.Id, () => RegisterInboundAsync(product.Id), () => operations.DeleteProductAsync(product.Id));
        var isActive = await queries.IsActiveAsync(product.Id);
        var stock = await queries.GetStockAsync(product.Id);
        isActive.ShouldSatisfyAllConditions(
            () => isActive.ShouldBeTrue(),
            () => stock.ShouldBe(InboundQuantity));
    }

    [Fact]
    public async Task RegisterMovement_WhenDeleteIsQueuedFirst_FailsWithProductInactive()
    {
        var product = await operations.CreateProductAsync(InventoryOperations.CreateUniqueSku());
        var (deleteOutcome, inboundOutcome) = await RunQueuedAsync(product.Id, () => operations.DeleteProductAsync(product.Id), () => RegisterInboundAsync(product.Id));
        inboundOutcome.ShouldSatisfyAllConditions(
            () => deleteOutcome.ShouldBeNull(),
            () => inboundOutcome.ShouldBeOfType<DomainException>().Code.ShouldBe(ErrorCode.ProductInactive));
    }

    [Fact]
    public async Task RegisterMovement_WhenDeleteIsQueuedFirst_LeavesProductDeletedWithoutStock()
    {
        var product = await operations.CreateProductAsync(InventoryOperations.CreateUniqueSku());
        await RunQueuedAsync(product.Id, () => operations.DeleteProductAsync(product.Id), () => RegisterInboundAsync(product.Id));
        var isActive = await queries.IsActiveAsync(product.Id);
        var ledger = await queries.GetLedgerAsync(product.Id);
        isActive.ShouldSatisfyAllConditions(
            () => isActive.ShouldBeFalse(),
            () => ledger.ShouldBeEmpty());
    }

    private async Task<Product> ReadDeletedSnapshotAsync(int productId)
    {
        await using var scope = await WriteScope.BeginAsync(fixture);
        var snapshot = await scope.Products.GetStockSnapshotAsync(productId, CancellationToken.None);
        snapshot.ShouldNotBeNull().Delete(DateTime.UtcNow, IntegrationTestUser.TestSubject);

        return snapshot;
    }

    private async Task<(Exception? First, Exception? Second)> RunQueuedAsync(int productId, Func<Task> first, Func<Task> second)
    {
        await using var rowLock = await ProductRowLock.AcquireAsync(fixture, productId);
        var firstOutcome = Record.ExceptionAsync(first);
        await rowLock.WaitForLockWaitersAsync(1);
        var secondOutcome = Record.ExceptionAsync(second);
        await rowLock.WaitForLockWaitersAsync(2);
        await rowLock.ReleaseAsync();

        return (await firstOutcome, await secondOutcome);
    }

    private async Task RegisterInboundAsync(int productId)
    {
        await operations.RegisterMovementAsync(productId, MovementType.In, InboundQuantity);
    }
}
