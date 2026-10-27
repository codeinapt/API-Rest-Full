using DbUp;
using Microsoft.Extensions.Logging;

namespace Inventory.Infrastructure.Migrations;

public sealed class DatabaseMigrator(string databaseConnectionString, ILogger<DatabaseMigrator> logger)
{
    public void Migrate()
    {
        EnsureDatabase.For.SqlDatabase(databaseConnectionString);
        var upgradeResult = DeployChanges.To
            .SqlDatabase(databaseConnectionString)
            .WithScriptsEmbeddedInAssembly(typeof(DatabaseMigrator).Assembly)
            .WithTransactionPerScript()
            .LogTo(logger)
            .Build()
            .PerformUpgrade();
        if (!upgradeResult.Successful)
        {
            throw new InvalidOperationException($"Database migration failed on script {upgradeResult.ErrorScript?.Name}.", upgradeResult.Error);
        }
    }
}
