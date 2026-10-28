using Inventory.Domain.Common;
using Inventory.Domain.Products;
using Inventory.IntegrationTests.Common;

namespace Inventory.IntegrationTests.Products;

public sealed class SkuUniquenessTests(SqlServerFixture fixture)
{
    private static readonly ProductDetails DuplicateDetails = new("Duplicate SKU product", null, 0);

    private readonly InventoryOperations operations = new(fixture);

    [Fact]
    public async Task InsertAsync_WhenActiveProductHasSameSku_ThrowsSkuAlreadyExists()
    {
        var existing = await operations.CreateProductAsync(InventoryOperations.CreateUniqueSku());
        await using var scope = await WriteScope.BeginAsync(fixture);
        var category = await scope.Categories.GetByIdForUpdateAsync(fixture.TestCategoryId, CancellationToken.None);
        var duplicate = Product.Create(category.ShouldNotBeNull(), existing.Sku, DuplicateDetails, DateTime.UtcNow);
        var exception = await Should.ThrowAsync<DomainException>(() => scope.Products.InsertAsync(duplicate, CancellationToken.None));
        exception.Code.ShouldBe(ErrorCode.SkuAlreadyExists);
    }

    [Fact]
    public async Task CreateProduct_WhenSameSkuBelongsToInactiveProduct_CreatesNewProduct()
    {
        var inactive = await operations.CreateProductAsync(InventoryOperations.CreateUniqueSku());
        await operations.DeactivateProductAsync(inactive.Id);
        var created = await operations.CreateProductAsync(inactive.Sku);
        created.Id.ShouldNotBe(inactive.Id);
    }

    [Fact]
    public async Task ReactivateAsync_WhenActiveProductHasSameSku_ThrowsSkuAlreadyExists()
    {
        var inactive = await operations.CreateProductAsync(InventoryOperations.CreateUniqueSku());
        await operations.DeactivateProductAsync(inactive.Id);
        await operations.CreateProductAsync(inactive.Sku);
        await using var scope = await WriteScope.BeginAsync(fixture);
        var reactivated = await ReadReactivatedSnapshotAsync(scope, inactive.Id);
        var exception = await Should.ThrowAsync<DomainException>(() => scope.Products.ReactivateAsync(reactivated, CancellationToken.None));
        exception.Code.ShouldBe(ErrorCode.SkuAlreadyExists);
    }

    [Fact]
    public async Task CreateProduct_WhenConcurrentRequestsShareSku_FailsAllButOneWithSkuAlreadyExists()
    {
        var sku = InventoryOperations.CreateUniqueSku();
        var outcomes = await Task.WhenAll(Record.ExceptionAsync(() => operations.CreateProductAsync(sku)).AsTask(), Record.ExceptionAsync(() => operations.CreateProductAsync(sku)).AsTask());
        outcomes.ShouldSatisfyAllConditions(
            () => outcomes.Count(outcome => outcome is null).ShouldBe(1),
            () => outcomes.OfType<DomainException>().Single().Code.ShouldBe(ErrorCode.SkuAlreadyExists));
    }

    private static async Task<Product> ReadReactivatedSnapshotAsync(WriteScope scope, int productId)
    {
        var product = await scope.Products.GetByIdForUpdateAsync(productId, CancellationToken.None);
        var category = await scope.Categories.GetByIdForUpdateAsync(product.ShouldNotBeNull().CategoryId, CancellationToken.None);
        product.Reactivate(category.ShouldNotBeNull(), DateTime.UtcNow, IntegrationTestUser.TestSubject);

        return product;
    }
}
