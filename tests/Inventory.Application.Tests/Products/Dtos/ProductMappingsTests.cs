using Inventory.Application.Products.Dtos;
using Inventory.Application.Tests.Common;
using Inventory.Domain.Areas;
using Inventory.Domain.Categories;
using Inventory.Domain.Products;

namespace Inventory.Application.Tests.Products.Dtos;

public sealed class ProductMappingsTests
{
    private const string Supervisor = "supervisor-1";
    private static readonly DateTime CreationTime = new(2026, 10, 27, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime DeactivationTime = new(2026, 10, 27, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime ReactivationTime = new(2026, 10, 27, 11, 0, 0, DateTimeKind.Utc);

    private readonly Area area = Area.Create("FOOD", "Food", CreationTime);
    private readonly Category category;

    public ProductMappingsTests()
    {
        category = PersistedIdentity.Assign(Category.Create(area, "Dairy", CreationTime), 7);
    }

    [Fact]
    public void ToResponse_WithActiveProduct_MapsFieldsWithoutDeactivationNorReactivation()
    {
        var product = CreateProduct(minimumStock: 5);
        var response = product.ToResponse(category, area);
        response.ShouldBe(new ProductResponse(21, "MILK-001", "Whole milk", "One litre bottle", 7, "Dairy", "FOOD", 0, 5, true, true, null, null, CreationTime, null));
    }

    [Fact]
    public void ToResponse_WithStockAboveMinimum_IsNotLowStock()
    {
        var product = AssignStock(CreateProduct(minimumStock: 2), 3);
        var response = product.ToResponse(category, area);
        response.IsLowStock.ShouldBeFalse();
    }

    [Fact]
    public void ToResponse_WithDeactivatedProduct_MapsDeactivation()
    {
        var product = CreateProduct(minimumStock: 0);
        product.Deactivate(new ProductDeactivation(DeactivationReason.Other, "Supplier recall", DeactivationTime, Supervisor));
        var response = product.ToResponse(category, area);
        response.IsActive.ShouldBeFalse();
        response.Deactivation.ShouldBe(new ProductDeactivationResponse(DeactivationReason.Other, "Supplier recall", DeactivationTime, Supervisor));
        response.Reactivation.ShouldBeNull();
    }

    [Fact]
    public void ToResponse_WithReactivatedProduct_MapsReactivationAndKeepsPreviousDeactivation()
    {
        var product = CreateProduct(minimumStock: 0);
        product.Deactivate(new ProductDeactivation(DeactivationReason.Expired, null, DeactivationTime, Supervisor));
        product.Reactivate(category, ReactivationTime, Supervisor);
        var response = product.ToResponse(category, area);
        response.Deactivation.ShouldBe(new ProductDeactivationResponse(DeactivationReason.Expired, null, DeactivationTime, Supervisor));
        response.Reactivation.ShouldBe(new ProductReactivationResponse(ReactivationTime, Supervisor));
    }

    [Fact]
    public void ToDeactivation_WithRequest_MapsReasonAndNotesWithTimeAndUser()
    {
        var request = new DeactivateProductRequest(DeactivationReason.Other, "Supplier recall");
        var deactivation = request.ToDeactivation(DeactivationTime, Supervisor);
        deactivation.ShouldBe(new ProductDeactivation(DeactivationReason.Other, "Supplier recall", DeactivationTime, Supervisor));
    }

    private static Product AssignStock(Product product, int stock)
    {
        typeof(Product).GetProperty(nameof(Product.Stock))!.SetValue(product, stock);

        return product;
    }

    private Product CreateProduct(int minimumStock)
    {
        var details = new ProductDetails("Whole milk", "One litre bottle", minimumStock);

        return PersistedIdentity.Assign(Product.Create(category, "MILK-001", details, CreationTime), 21);
    }
}
