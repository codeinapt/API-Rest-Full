using Inventory.Application.Common.Authorization;
using Inventory.Application.Common.Interfaces;
using Inventory.Infrastructure;
using Inventory.Infrastructure.Migrations;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Testcontainers.MsSql;

namespace Inventory.IntegrationTests.Common;

public sealed class SqlServerFixture : IAsyncLifetime
{
    public const string TestAreaCode = "ITEST";

    private const string SqlServerImage = "mcr.microsoft.com/mssql/server:2022-CU27-GDR1-ubuntu-22.04";
    private const string DatabaseName = "InventoryIntegrationTests";

    private readonly MsSqlContainer container = new MsSqlBuilder(SqlServerImage).Build();
    private ServiceProvider? serviceProvider;

    public string ConnectionString { get; private set; } = string.Empty;

    public IServiceProvider Services => serviceProvider ?? throw new InvalidOperationException("The SQL Server fixture is not initialized.");

    public int TestCategoryId { get; private set; }

    public async ValueTask InitializeAsync()
    {
        await container.StartAsync();
        ConnectionString = BuildDatabaseConnectionString();
        CreateMigrator().Migrate();
        serviceProvider = BuildServiceProvider();
        TestCategoryId = await CreateTestCategoryAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (serviceProvider is not null)
        {
            await serviceProvider.DisposeAsync();
        }
        await container.DisposeAsync();
    }

    public DatabaseMigrator CreateMigrator()
    {
        return new DatabaseMigrator(ConnectionString, NullLogger<DatabaseMigrator>.Instance);
    }

    private string BuildDatabaseConnectionString()
    {
        var builder = new SqlConnectionStringBuilder(container.GetConnectionString()) { InitialCatalog = DatabaseName };

        return builder.ConnectionString;
    }

    private ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInventoryInfrastructure(ConnectionString);
        services.AddSingleton<ICurrentUser, IntegrationTestUser>();
        services.AddScoped<AreaAccessPolicy>();

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private async Task<int> CreateTestCategoryAsync()
    {
        var operations = new InventoryOperations(this);
        var area = await operations.CreateAreaAsync(TestAreaCode);
        var category = await operations.CreateCategoryAsync(area.Id);

        return category.Id;
    }
}
