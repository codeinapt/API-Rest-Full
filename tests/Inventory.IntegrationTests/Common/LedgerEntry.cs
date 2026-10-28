namespace Inventory.IntegrationTests.Common;

internal sealed record LedgerEntry(int Delta, int PreviousStock, int ResultingStock);
