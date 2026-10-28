using Dapper;
using Inventory.Application.Products.Ports;
using Inventory.Domain.Common;
using Inventory.Domain.Products;
using Microsoft.Data.SqlClient;

namespace Inventory.Infrastructure.Persistence.Write;

internal sealed class ProductWriteRepository(UnitOfWork unitOfWork) : IProductWriteRepository
{
    private const int UniqueIndexViolation = 2601;
    private const int UniqueConstraintViolation = 2627;

    private const string SelectByIdForUpdateSql = """
        SELECT Id, Sku, Name, Description, CategoryId, Stock, MinimumStock, IsActive,
               DeactivationReason, DeactivationNotes, DeactivatedAt, DeactivatedBy, ReactivatedAt, ReactivatedBy, CreatedAt, UpdatedAt
        FROM dbo.Products WITH (UPDLOCK, HOLDLOCK)
        WHERE Id = @ProductId;
        """;

    private const string SelectSnapshotWithAreaForUpdateSql = """
        SELECT p.Id, p.Sku, p.Name, p.Description, p.CategoryId, p.Stock, p.MinimumStock, p.IsActive,
               p.DeactivationReason, p.DeactivationNotes, p.DeactivatedAt, p.DeactivatedBy, p.ReactivatedAt, p.ReactivatedBy, p.CreatedAt, p.UpdatedAt,
               a.Code AS AreaCode
        FROM dbo.Products p WITH (UPDLOCK, HOLDLOCK)
        INNER JOIN dbo.Categories c ON c.Id = p.CategoryId
        INNER JOIN dbo.Areas a ON a.Id = c.AreaId
        WHERE p.Id = @ProductId;
        """;

    private const string ExistsOtherActiveProductWithSkuSql = """
        SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.Products WHERE Sku = @Sku AND IsActive = 1 AND Id <> @Id) THEN 1 ELSE 0 END;
        """;

    private const string InsertSql = """
        INSERT INTO dbo.Products (Sku, Name, Description, CategoryId, MinimumStock, IsActive, CreatedAt)
        OUTPUT inserted.Id, inserted.Sku, inserted.Name, inserted.Description, inserted.CategoryId, inserted.Stock, inserted.MinimumStock, inserted.IsActive,
               inserted.DeactivationReason, inserted.DeactivationNotes, inserted.DeactivatedAt, inserted.DeactivatedBy, inserted.ReactivatedAt, inserted.ReactivatedBy,
               inserted.CreatedAt, inserted.UpdatedAt
        VALUES (@Sku, @Name, @Description, @CategoryId, @MinimumStock, @IsActive, @CreatedAt);
        """;

    private const string UpdateDetailsSql = """
        UPDATE dbo.Products
        SET Name = @Name, Description = @Description, MinimumStock = @MinimumStock, UpdatedAt = @UpdatedAt
        WHERE Id = @Id;
        """;

    private const string UpdateCategorySql = """
        UPDATE dbo.Products
        SET CategoryId = @CategoryId, UpdatedAt = @UpdatedAt
        WHERE Id = @Id;
        """;

    private const string DeleteSql = """
        UPDATE dbo.Products
        SET IsActive = 0, DeactivationReason = @DeactivationReason, DeactivationNotes = @DeactivationNotes, DeactivatedAt = @DeactivatedAt, DeactivatedBy = @DeactivatedBy
        WHERE Id = @Id AND IsActive = 1 AND Stock = 0;
        """;

    private const string SaveDeactivationSql = """
        UPDATE dbo.Products
        SET IsActive = 0, DeactivationReason = @DeactivationReason, DeactivationNotes = @DeactivationNotes, DeactivatedAt = @DeactivatedAt, DeactivatedBy = @DeactivatedBy
        WHERE Id = @Id AND IsActive = 1;
        """;

    private const string ReactivateSql = """
        UPDATE p
        SET IsActive = 1, ReactivatedAt = @ReactivatedAt, ReactivatedBy = @ReactivatedBy
        FROM dbo.Products p
        WHERE p.Id = @Id AND p.IsActive = 0
          AND EXISTS (SELECT 1 FROM dbo.Categories c WHERE c.Id = p.CategoryId AND c.IsActive = 1);
        """;

    public async Task<Product?> GetByIdForUpdateAsync(int productId, CancellationToken cancellationToken)
    {
        return await unitOfWork.Connection.QuerySingleOrDefaultAsync<Product>(CreateCommand(SelectByIdForUpdateSql, new { ProductId = productId }, cancellationToken));
    }

    public async Task<ProductAreaSnapshot?> GetSnapshotWithAreaForUpdateAsync(int productId, CancellationToken cancellationToken)
    {
        var command = CreateCommand(SelectSnapshotWithAreaForUpdateSql, new { ProductId = productId }, cancellationToken);
        var snapshots = await unitOfWork.Connection.QueryAsync<Product, string, ProductAreaSnapshot>(command, (product, areaCode) => new ProductAreaSnapshot(product, areaCode), splitOn: "AreaCode");

        return snapshots.SingleOrDefault();
    }

    public async Task<bool> ExistsOtherActiveProductWithSkuAsync(Product product, CancellationToken cancellationToken)
    {
        var parameters = new { Sku = ToAnsiSku(product.Sku), product.Id };

        return await unitOfWork.Connection.ExecuteScalarAsync<bool>(CreateCommand(ExistsOtherActiveProductWithSkuSql, parameters, cancellationToken));
    }

    public async Task<Product> InsertAsync(Product product, CancellationToken cancellationToken)
    {
        var parameters = new { Sku = ToAnsiSku(product.Sku), product.Name, product.Description, product.CategoryId, product.MinimumStock, product.IsActive, product.CreatedAt };
        try
        {
            return await unitOfWork.Connection.QuerySingleAsync<Product>(CreateCommand(InsertSql, parameters, cancellationToken));
        }
        catch (SqlException exception) when (exception.Number is UniqueIndexViolation or UniqueConstraintViolation)
        {
            throw new DomainException(ErrorCode.SkuAlreadyExists, product.Sku);
        }
    }

    public async Task UpdateDetailsAsync(Product product, CancellationToken cancellationToken)
    {
        var parameters = new { product.Id, product.Name, product.Description, product.MinimumStock, product.UpdatedAt };
        await unitOfWork.Connection.ExecuteAsync(CreateCommand(UpdateDetailsSql, parameters, cancellationToken));
    }

    public async Task UpdateCategoryAsync(Product product, CancellationToken cancellationToken)
    {
        var parameters = new { product.Id, product.CategoryId, product.UpdatedAt };
        await unitOfWork.Connection.ExecuteAsync(CreateCommand(UpdateCategorySql, parameters, cancellationToken));
    }

    public async Task<bool> DeleteAsync(Product product, CancellationToken cancellationToken)
    {
        return await ExecuteDeactivationAsync(DeleteSql, product, cancellationToken);
    }

    public async Task<bool> SaveDeactivationAsync(Product product, CancellationToken cancellationToken)
    {
        return await ExecuteDeactivationAsync(SaveDeactivationSql, product, cancellationToken);
    }

    public async Task<bool> ReactivateAsync(Product product, CancellationToken cancellationToken)
    {
        var parameters = new { product.Id, product.ReactivatedAt, product.ReactivatedBy };
        try
        {
            var affectedRows = await unitOfWork.Connection.ExecuteAsync(CreateCommand(ReactivateSql, parameters, cancellationToken));

            return affectedRows == 1;
        }
        catch (SqlException exception) when (exception.Number is UniqueIndexViolation or UniqueConstraintViolation)
        {
            throw new DomainException(ErrorCode.SkuAlreadyExists, product.Sku);
        }
    }

    private static DbString ToAnsiSku(string sku)
    {
        return new DbString { Value = sku, IsAnsi = true, Length = Product.SkuMaximumLength };
    }

    private async Task<bool> ExecuteDeactivationAsync(string sql, Product product, CancellationToken cancellationToken)
    {
        var parameters = new { product.Id, DeactivationReason = (byte?)product.DeactivationReason, product.DeactivationNotes, product.DeactivatedAt, product.DeactivatedBy };
        var affectedRows = await unitOfWork.Connection.ExecuteAsync(CreateCommand(sql, parameters, cancellationToken));

        return affectedRows == 1;
    }

    private CommandDefinition CreateCommand(string sql, object parameters, CancellationToken cancellationToken)
    {
        return new CommandDefinition(sql, parameters, unitOfWork.Transaction, cancellationToken: cancellationToken);
    }
}
