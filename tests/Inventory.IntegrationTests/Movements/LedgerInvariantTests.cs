using Inventory.Domain.Movements;
using Inventory.IntegrationTests.Common;

namespace Inventory.IntegrationTests.Movements;

public sealed class LedgerInvariantTests(SqlServerFixture fixture)
{
    private const int InboundQuantity = 10;
    private const int OutboundQuantity = 4;
    private const int AdjustmentDelta = -1;
    private const int InitialStock = 10;
    private const int BurstSize = 20;
    private const int BurstOutboundQuantity = 1;

    private readonly InventoryOperations operations = new(fixture);
    private readonly DatabaseQueries queries = new(fixture);

    [Fact]
    public async Task RegisterMovementsAndAdjustment_InSequence_KeepStockEqualToSumOfDeltas()
    {
        var product = await operations.CreateProductAsync(InventoryOperations.CreateUniqueSku());
        await operations.RegisterMovementAsync(product.Id, MovementType.In, InboundQuantity);
        await operations.RegisterMovementAsync(product.Id, MovementType.Out, OutboundQuantity);
        await operations.RegisterAdjustmentAsync(product.Id, AdjustmentDelta);
        var ledger = await queries.GetLedgerAsync(product.Id);
        (await queries.GetStockAsync(product.Id)).ShouldBe(ledger.Sum(entry => entry.Delta));
    }

    [Fact]
    public async Task RegisterMovement_WithConcurrentOutboundBurst_KeepsStockEqualToSumOfDeltas()
    {
        var productId = await operations.CreateProductWithStockAsync(InitialStock);
        await operations.RegisterConcurrentOutboundsAsync(productId, BurstSize, BurstOutboundQuantity);
        var ledger = await queries.GetLedgerAsync(productId);
        (await queries.GetStockAsync(productId)).ShouldBe(ledger.Sum(entry => entry.Delta));
    }

    [Fact]
    public async Task RegisterMovement_WithConcurrentOutboundBurst_ChainsEachPreviousStockToTheLastResultingStock()
    {
        var productId = await operations.CreateProductWithStockAsync(InitialStock);
        await operations.RegisterConcurrentOutboundsAsync(productId, BurstSize, BurstOutboundQuantity);
        var ledger = await queries.GetLedgerAsync(productId);
        var previousStocks = ledger.Skip(1).Select(entry => entry.PreviousStock);
        previousStocks.ShouldBe(ledger.SkipLast(1).Select(entry => entry.ResultingStock));
    }
}
