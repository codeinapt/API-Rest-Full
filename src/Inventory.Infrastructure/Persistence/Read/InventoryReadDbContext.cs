using Inventory.Infrastructure.Persistence.Read.Configurations;
using Inventory.Infrastructure.Persistence.Read.Models;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence.Read;

internal sealed class InventoryReadDbContext(DbContextOptions<InventoryReadDbContext> options) : DbContext(options)
{
    private const string ReadOnlyContextMessage = "InventoryReadDbContext is read-only: writes go through Dapper write repositories.";

    public DbSet<AreaRow> Areas => Set<AreaRow>();

    public DbSet<CategoryRow> Categories => Set<CategoryRow>();

    public DbSet<ProductRow> Products => Set<ProductRow>();

    public DbSet<InventoryMovementRow> InventoryMovements => Set<InventoryMovementRow>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        throw new InvalidOperationException(ReadOnlyContextMessage);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException(ReadOnlyContextMessage);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InventoryReadDbContext).Assembly);
    }
}
