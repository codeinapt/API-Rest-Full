using Dapper;
using Inventory.Application.Areas.Ports;
using Inventory.Domain.Areas;
using Inventory.Domain.Common;
using Microsoft.Data.SqlClient;

namespace Inventory.Infrastructure.Persistence.Write;

internal sealed class AreaWriteRepository(UnitOfWork unitOfWork) : IAreaWriteRepository
{
    private const int UniqueIndexViolation = 2601;
    private const int UniqueConstraintViolation = 2627;

    private const string SelectByIdForUpdateSql = """
        SELECT Id, Code, Name, IsActive, CreatedAt, UpdatedAt
        FROM dbo.Areas WITH (UPDLOCK, HOLDLOCK)
        WHERE Id = @AreaId;
        """;

    private const string ExistsActiveCodeSql = """
        SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.Areas WHERE Code = @Code AND IsActive = 1) THEN 1 ELSE 0 END;
        """;

    private const string CountActiveCategoriesSql = """
        SELECT COUNT(*) FROM dbo.Categories WHERE AreaId = @AreaId AND IsActive = 1;
        """;

    private const string InsertSql = """
        INSERT INTO dbo.Areas (Code, Name, IsActive, CreatedAt)
        OUTPUT inserted.Id, inserted.Code, inserted.Name, inserted.IsActive, inserted.CreatedAt, inserted.UpdatedAt
        VALUES (@Code, @Name, @IsActive, @CreatedAt);
        """;

    private const string UpdateSql = """
        UPDATE dbo.Areas
        SET Name = @Name, IsActive = @IsActive, UpdatedAt = @UpdatedAt
        WHERE Id = @Id;
        """;

    public async Task<Area?> GetByIdForUpdateAsync(int areaId, CancellationToken cancellationToken)
    {
        return await unitOfWork.Connection.QuerySingleOrDefaultAsync<Area>(CreateCommand(SelectByIdForUpdateSql, new { AreaId = areaId }, cancellationToken));
    }

    public async Task<bool> ExistsActiveCodeAsync(string code, CancellationToken cancellationToken)
    {
        return await unitOfWork.Connection.ExecuteScalarAsync<bool>(CreateCommand(ExistsActiveCodeSql, new { Code = ToAnsiCode(code) }, cancellationToken));
    }

    public async Task<int> CountActiveCategoriesAsync(int areaId, CancellationToken cancellationToken)
    {
        return await unitOfWork.Connection.ExecuteScalarAsync<int>(CreateCommand(CountActiveCategoriesSql, new { AreaId = areaId }, cancellationToken));
    }

    public async Task<Area> InsertAsync(Area area, CancellationToken cancellationToken)
    {
        var parameters = new { Code = ToAnsiCode(area.Code), area.Name, area.IsActive, area.CreatedAt };
        try
        {
            return await unitOfWork.Connection.QuerySingleAsync<Area>(CreateCommand(InsertSql, parameters, cancellationToken));
        }
        catch (SqlException exception) when (exception.Number is UniqueIndexViolation or UniqueConstraintViolation)
        {
            throw new DomainException(ErrorCode.AreaCodeAlreadyExists, area.Code);
        }
    }

    public async Task UpdateAsync(Area area, CancellationToken cancellationToken)
    {
        await unitOfWork.Connection.ExecuteAsync(CreateCommand(UpdateSql, area, cancellationToken));
    }

    private static DbString ToAnsiCode(string code)
    {
        return new DbString { Value = code, IsAnsi = true, Length = Area.CodeMaximumLength };
    }

    private CommandDefinition CreateCommand(string sql, object parameters, CancellationToken cancellationToken)
    {
        return new CommandDefinition(sql, parameters, unitOfWork.Transaction, cancellationToken: cancellationToken);
    }
}
