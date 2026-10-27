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

    public async Task<Product?> GetByIdForUpdateAsync(int productId, CancellationToken cancellationToken)
    {
        return await unitOfWork.Connection.QuerySingleOrDefaultAsync<Product>(CreateCommand(SelectByIdForUpdateSql, new { ProductId = productId }, cancellationToken));
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

    private static DbString ToAnsiSku(string sku)
    {
        return new DbString { Value = sku, IsAnsi = true, Length = Product.SkuMaximumLength };
    }

    private CommandDefinition CreateCommand(string sql, object parameters, CancellationToken cancellationToken)
    {
        return new CommandDefinition(sql, parameters, unitOfWork.Transaction, cancellationToken: cancellationToken);
    }
}
