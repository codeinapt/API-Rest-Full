using Inventory.Infrastructure.Migrations;
using Inventory.IntegrationTests.Common;

namespace Inventory.IntegrationTests.Migrations;

public sealed class DatabaseMigratorTests(SqlServerFixture fixture)
{
    private const string ScriptResourceExtension = ".sql";

    private readonly DatabaseQueries queries = new(fixture);

    [Fact]
    public async Task Migrate_WhenDatabaseIsUpToDate_KeepsOneJournalEntryPerScript()
    {
        fixture.CreateMigrator().Migrate();
        (await queries.CountJournalEntriesAsync()).ShouldBe(CountEmbeddedScripts());
    }

    [Fact]
    public async Task Migrate_OnEmptyDatabase_SeedsProductsWithStock()
    {
        (await queries.CountSeededProductsWithStockAsync()).ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Migrate_OnEmptyDatabase_SeedsStockEqualToSumOfDeltas()
    {
        (await queries.CountSeededProductsWithUnbalancedLedgerAsync()).ShouldBe(0);
    }

    private static int CountEmbeddedScripts()
    {
        return typeof(DatabaseMigrator).Assembly.GetManifestResourceNames()
            .Count(resourceName => resourceName.EndsWith(ScriptResourceExtension, StringComparison.Ordinal));
    }
}
