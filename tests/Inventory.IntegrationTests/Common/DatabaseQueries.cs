using Dapper;
using Microsoft.Data.SqlClient;

namespace Inventory.IntegrationTests.Common;

internal sealed class DatabaseQueries(SqlServerFixture fixture)
{
    private const string SelectStockSql = "SELECT Stock FROM dbo.Products WHERE Id = @ProductId;";

    private const string SelectIsActiveSql = "SELECT IsActive FROM dbo.Products WHERE Id = @ProductId;";

    private const string SelectLedgerSql = """
        SELECT Delta, PreviousStock, ResultingStock FROM dbo.InventoryMovements
        WHERE ProductId = @ProductId
        ORDER BY Id;
        """;

    private const string CountStockedProductsOutsideCategorySql = """
        SELECT COUNT(*) FROM dbo.Products WHERE CategoryId <> @CategoryId AND Stock > 0;
        """;

    private const string CountUnbalancedProductsOutsideCategorySql = """
        SELECT COUNT(*) FROM dbo.Products p
        WHERE p.CategoryId <> @CategoryId
          AND p.Stock <> (SELECT ISNULL(SUM(m.Delta), 0) FROM dbo.InventoryMovements m WHERE m.ProductId = p.Id);
        """;

    private const string CountJournalEntriesSql = "SELECT COUNT(*) FROM dbo.SchemaVersions;";

    public async Task<int> GetStockAsync(int productId)
    {
        await using var connection = new SqlConnection(fixture.ConnectionString);

        return await connection.ExecuteScalarAsync<int>(SelectStockSql, new { ProductId = productId });
    }

    public async Task<bool> IsActiveAsync(int productId)
    {
        await using var connection = new SqlConnection(fixture.ConnectionString);

        return await connection.ExecuteScalarAsync<bool>(SelectIsActiveSql, new { ProductId = productId });
    }

    public async Task<IReadOnlyList<LedgerEntry>> GetLedgerAsync(int productId)
    {
        await using var connection = new SqlConnection(fixture.ConnectionString);
        var entries = await connection.QueryAsync<LedgerEntry>(SelectLedgerSql, new { ProductId = productId });

        return entries.ToList();
    }

    public async Task<int> CountSeededProductsWithStockAsync()
    {
        return await CountOutsideTestCategoryAsync(CountStockedProductsOutsideCategorySql);
    }

    public async Task<int> CountSeededProductsWithUnbalancedLedgerAsync()
    {
        return await CountOutsideTestCategoryAsync(CountUnbalancedProductsOutsideCategorySql);
    }

    public async Task<int> CountJournalEntriesAsync()
    {
        await using var connection = new SqlConnection(fixture.ConnectionString);

        return await connection.ExecuteScalarAsync<int>(CountJournalEntriesSql);
    }

    private async Task<int> CountOutsideTestCategoryAsync(string sql)
    {
        await using var connection = new SqlConnection(fixture.ConnectionString);

        return await connection.ExecuteScalarAsync<int>(sql, new { CategoryId = fixture.TestCategoryId });
    }
}
