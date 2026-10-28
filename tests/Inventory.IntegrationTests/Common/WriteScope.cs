using Inventory.Application.Categories.Ports;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Products.Ports;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.IntegrationTests.Common;

internal sealed class WriteScope : IAsyncDisposable
{
    private readonly AsyncServiceScope scope;

    private WriteScope(AsyncServiceScope scope)
    {
        this.scope = scope;
    }

    public IProductWriteRepository Products => scope.ServiceProvider.GetRequiredService<IProductWriteRepository>();

    public ICategoryWriteRepository Categories => scope.ServiceProvider.GetRequiredService<ICategoryWriteRepository>();

    public static async Task<WriteScope> BeginAsync(SqlServerFixture fixture)
    {
        var scope = fixture.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().BeginTransactionAsync(CancellationToken.None);

        return new WriteScope(scope);
    }

    public async ValueTask DisposeAsync()
    {
        await scope.DisposeAsync();
    }
}
