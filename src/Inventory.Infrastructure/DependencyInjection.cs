using Inventory.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Inventory.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInventoryInfrastructure(this IServiceCollection services, string databaseConnectionString)
    {
        services.AddSingleton(serviceProvider => new DatabaseMigrator(databaseConnectionString, serviceProvider.GetRequiredService<ILogger<DatabaseMigrator>>()));

        return services;
    }
}
