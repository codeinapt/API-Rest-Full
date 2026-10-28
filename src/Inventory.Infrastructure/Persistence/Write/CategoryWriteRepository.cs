using Dapper;
using Inventory.Application.Categories.Ports;
using Inventory.Domain.Categories;
using Inventory.Domain.Common;
using Microsoft.Data.SqlClient;

namespace Inventory.Infrastructure.Persistence.Write;

internal sealed class CategoryWriteRepository(UnitOfWork unitOfWork) : ICategoryWriteRepository
{
    private const int UniqueIndexViolation = 2601;
    private const int UniqueConstraintViolation = 2627;

    private const string SelectByIdForUpdateSql = """
        SELECT Id, AreaId, Name, IsActive, CreatedAt, UpdatedAt
        FROM dbo.Categories WITH (UPDLOCK, HOLDLOCK)
        WHERE Id = @CategoryId;
        """;

    private const string ExistsOtherActiveCategoryWithNameSql = """
        SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.Categories WHERE Name = @Name AND IsActive = 1 AND Id <> @Id) THEN 1 ELSE 0 END;
        """;

    private const string CountActiveProductsSql = """
        SELECT COUNT(*) FROM dbo.Products WHERE CategoryId = @CategoryId AND IsActive = 1;
        """;

    private const string InsertSql = """
        INSERT INTO dbo.Categories (AreaId, Name, IsActive, CreatedAt)
        OUTPUT inserted.Id, inserted.AreaId, inserted.Name, inserted.IsActive, inserted.CreatedAt, inserted.UpdatedAt
        VALUES (@AreaId, @Name, @IsActive, @CreatedAt);
        """;

    private const string UpdateSql = """
        UPDATE dbo.Categories
        SET AreaId = @AreaId, Name = @Name, IsActive = @IsActive, UpdatedAt = @UpdatedAt
        WHERE Id = @Id;
        """;

    public async Task<Category?> GetByIdForUpdateAsync(int categoryId, CancellationToken cancellationToken)
    {
        return await unitOfWork.Connection.QuerySingleOrDefaultAsync<Category>(CreateCommand(SelectByIdForUpdateSql, new { CategoryId = categoryId }, cancellationToken));
    }

    public async Task<bool> ExistsOtherActiveCategoryWithNameAsync(Category category, CancellationToken cancellationToken)
    {
        return await unitOfWork.Connection.ExecuteScalarAsync<bool>(CreateCommand(ExistsOtherActiveCategoryWithNameSql, new { category.Name, category.Id }, cancellationToken));
    }

    public async Task<int> CountActiveProductsAsync(int categoryId, CancellationToken cancellationToken)
    {
        return await unitOfWork.Connection.ExecuteScalarAsync<int>(CreateCommand(CountActiveProductsSql, new { CategoryId = categoryId }, cancellationToken));
    }

    public async Task<Category> InsertAsync(Category category, CancellationToken cancellationToken)
    {
        try
        {
            return await unitOfWork.Connection.QuerySingleAsync<Category>(CreateCommand(InsertSql, category, cancellationToken));
        }
        catch (SqlException exception) when (IsDuplicateName(exception))
        {
            throw new DomainException(ErrorCode.CategoryNameAlreadyExists, category.Name);
        }
    }

    public async Task UpdateAsync(Category category, CancellationToken cancellationToken)
    {
        try
        {
            await unitOfWork.Connection.ExecuteAsync(CreateCommand(UpdateSql, category, cancellationToken));
        }
        catch (SqlException exception) when (IsDuplicateName(exception))
        {
            throw new DomainException(ErrorCode.CategoryNameAlreadyExists, category.Name);
        }
    }

    private static bool IsDuplicateName(SqlException exception)
    {
        return exception.Number is UniqueIndexViolation or UniqueConstraintViolation;
    }

    private CommandDefinition CreateCommand(string sql, object parameters, CancellationToken cancellationToken)
    {
        return new CommandDefinition(sql, parameters, unitOfWork.Transaction, cancellationToken: cancellationToken);
    }
}
