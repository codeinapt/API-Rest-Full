using Inventory.Domain.Common;
using Inventory.Domain.Movements;
using Inventory.IntegrationTests.Common;

namespace Inventory.IntegrationTests.Movements;

public sealed class ConcurrentOutboundMovementTests(SqlServerFixture fixture)
{
    private const int InitialStock = 10;
    private const int QueuedOutboundQuantity = 7;
    private const int StockAfterOneQueuedOutbound = 3;
    private const int BurstSize = 20;
    private const int BurstOutboundQuantity = 1;

    private readonly InventoryOperations operations = new(fixture);
    private readonly DatabaseQueries queries = new(fixture);

    [Fact]
    public async Task RegisterMovement_WhenQueuedOutboundsTogetherExceedStock_AppliesOnlyOne()
    {
        var productId = await operations.CreateProductWithStockAsync(InitialStock);
        var outcomes = await RegisterQueuedOutboundsAsync(productId);
        outcomes.Count(outcome => outcome is null).ShouldBe(1);
    }

    [Fact]
    public async Task RegisterMovement_WhenQueuedOutboundsTogetherExceedStock_RejectsTheOtherWithInsufficientStock()
    {
        var productId = await operations.CreateProductWithStockAsync(InitialStock);
        var outcomes = await RegisterQueuedOutboundsAsync(productId);
        var rejection = outcomes.OfType<DomainException>().Single();
        rejection.ShouldSatisfyAllConditions(
            () => rejection.Code.ShouldBe(ErrorCode.InsufficientStock),
            () => rejection.MessageArguments.ShouldBe([StockAfterOneQueuedOutbound, QueuedOutboundQuantity]));
    }

    [Fact]
    public async Task RegisterMovement_WhenQueuedOutboundsTogetherExceedStock_KeepsStockOfTheAppliedOutbound()
    {
        var productId = await operations.CreateProductWithStockAsync(InitialStock);
        await RegisterQueuedOutboundsAsync(productId);
        (await queries.GetStockAsync(productId)).ShouldBe(StockAfterOneQueuedOutbound);
    }

    [Fact]
    public async Task RegisterMovement_WithConcurrentOutboundBurst_StopsAtZeroStock()
    {
        var productId = await operations.CreateProductWithStockAsync(InitialStock);
        await operations.RegisterConcurrentOutboundsAsync(productId, BurstSize, BurstOutboundQuantity);
        (await queries.GetStockAsync(productId)).ShouldBe(0);
    }

    [Fact]
    public async Task RegisterMovement_WithConcurrentOutboundBurst_RejectsEveryOutboundBeyondStock()
    {
        var productId = await operations.CreateProductWithStockAsync(InitialStock);
        var outcomes = await operations.RegisterConcurrentOutboundsAsync(productId, BurstSize, BurstOutboundQuantity);
        outcomes.OfType<DomainException>().Count(rejection => rejection.Code == ErrorCode.InsufficientStock).ShouldBe(BurstSize - InitialStock);
    }

    private async Task<Exception?[]> RegisterQueuedOutboundsAsync(int productId)
    {
        await using var rowLock = await ProductRowLock.AcquireAsync(fixture, productId);
        var outbounds = new[] { RegisterQueuedOutboundAsync(productId), RegisterQueuedOutboundAsync(productId) };
        await rowLock.WaitForLockWaitersAsync(outbounds.Length);
        await rowLock.ReleaseAsync();

        return await Task.WhenAll(outbounds);
    }

    private Task<Exception?> RegisterQueuedOutboundAsync(int productId)
    {
        return Record.ExceptionAsync(() => operations.RegisterMovementAsync(productId, MovementType.Out, QueuedOutboundQuantity)).AsTask();
    }
}
