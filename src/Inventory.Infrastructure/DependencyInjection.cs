using Inventory.Application.Areas.Ports;
using Inventory.Application.Categories.Ports;
using Inventory.Application.Common.Interfaces;
using Inventory.Infrastructure.Common;
using Inventory.Infrastructure.Migrations;
using Inventory.Infrastructure.Persistence.Read;
using Inventory.Infrastructure.Persistence.Write;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Inventory.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInventoryInfrastructure(this IServiceCollection services, string databaseConnectionString)
    {
        services.AddSingleton(serviceProvider => new DatabaseMigrator(databaseConnectionString, serviceProvider.GetRequiredService<ILogger<DatabaseMigrator>>()));
        services.AddSingleton<IClock, SystemClock>();
        services.AddDbContext<InventoryReadDbContext>(options => options.UseSqlServer(databaseConnectionString));
        services.AddSingleton(new SqlConnectionFactory(databaseConnectionString));
        services.AddScoped<UnitOfWork>();
        services.AddScoped<IUnitOfWork>(serviceProvider => serviceProvider.GetRequiredService<UnitOfWork>());
        services.AddScoped<IAreaReadRepository, AreaReadRepository>();
        services.AddScoped<IAreaWriteRepository, AreaWriteRepository>();
        services.AddScoped<ICategoryReadRepository, CategoryReadRepository>();
        services.AddScoped<ICategoryWriteRepository, CategoryWriteRepository>();

        return services;
    }
}
