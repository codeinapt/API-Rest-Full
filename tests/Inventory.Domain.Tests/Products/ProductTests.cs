using Inventory.Domain.Areas;
using Inventory.Domain.Categories;
using Inventory.Domain.Common;
using Inventory.Domain.Products;
using Inventory.Domain.Tests.Common;

namespace Inventory.Domain.Tests.Products;

public sealed class ProductTests
{
    private const string ValidSku = "MILK-1L";
    private const string UserId = "user-sub";
    private const int ProductId = 100;
    private static readonly DateTime CreationTime = new(2026, 10, 27, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime UpdateTime = new(2026, 10, 28, 9, 0, 0, DateTimeKind.Utc);
    private static readonly ProductDetails ValidDetails = new("Whole milk", "One liter carton", 5);

    public static TheoryData<string> InvalidSkus()
    {
        return new TheoryData<string>
        {
            string.Empty,
            "AB",
            new string('A', Product.SkuMaximumLength + 1),
            "milk-1l",
            "MILK 1L",
            "MILK_1L",
            "MILK-1L\n",
        };
    }

    public static TheoryData<ProductDetails> InvalidDetails()
    {
        return new TheoryData<ProductDetails>
        {
            new ProductDetails(string.Empty, null, 0),
            new ProductDetails("   ", null, 0),
            new ProductDetails("No", null, 0),
            new ProductDetails(new string('N', Product.NameMaximumLength + 1), null, 0),
            new ProductDetails("Whole milk", new string('D', Product.DescriptionMaximumLength + 1), 0),
            new ProductDetails("Whole milk", null, -1),
        };
    }

    public static TheoryData<ProductDeactivation> InvalidDeactivations()
    {
        return new TheoryData<ProductDeactivation>
        {
            new ProductDeactivation((DeactivationReason)0, null, UpdateTime, UserId),
            new ProductDeactivation((DeactivationReason)6, null, UpdateTime, UserId),
            new ProductDeactivation(DeactivationReason.Other, null, UpdateTime, UserId),
            new ProductDeactivation(DeactivationReason.Other, "   ", UpdateTime, UserId),
            new ProductDeactivation(DeactivationReason.Expired, new string('N', Product.DeactivationNotesMaximumLength + 1), UpdateTime, UserId),
        };
    }

    [Fact]
    public void Create_WithValidData_ReturnsActiveProductWithZeroStock()
    {
        var product = Product.Create(CreateActiveCategory(10), ValidSku, ValidDetails, CreationTime);
        product.Sku.ShouldBe(ValidSku);
        product.Name.ShouldBe(ValidDetails.Name);
        product.Description.ShouldBe(ValidDetails.Description);
        product.MinimumStock.ShouldBe(ValidDetails.MinimumStock);
        product.CategoryId.ShouldBe(10);
        product.Stock.ShouldBe(0);
        product.IsActive.ShouldBeTrue();
        product.CreatedAt.ShouldBe(CreationTime);
        product.UpdatedAt.ShouldBeNull();
    }

    [Fact]
    public void Create_WithValidData_HasNoDeactivationOrReactivationData()
    {
        var product = Product.Create(CreateActiveCategory(10), ValidSku, ValidDetails, CreationTime);
        product.DeactivationReason.ShouldBeNull();
        product.DeactivationNotes.ShouldBeNull();
        product.DeactivatedAt.ShouldBeNull();
        product.DeactivatedBy.ShouldBeNull();
        product.ReactivatedAt.ShouldBeNull();
        product.ReactivatedBy.ShouldBeNull();
    }

    [Theory]
    [InlineData("ABC")]
    [InlineData("SKU-0001")]
    [InlineData("12345678901234567890123456789012345678901234567890")]
    public void Create_WithValidSku_Succeeds(string sku)
    {
        var product = Product.Create(CreateActiveCategory(10), sku, ValidDetails, CreationTime);
        product.Sku.ShouldBe(sku);
    }

    [Theory]
    [MemberData(nameof(InvalidSkus))]
    public void Create_WithInvalidSku_ThrowsValidationError(string sku)
    {
        var category = CreateActiveCategory(10);
        var exception = Should.Throw<DomainException>(() => Product.Create(category, sku, ValidDetails, CreationTime));
        exception.Code.ShouldBe(ErrorCode.ValidationError);
    }

    [Fact]
    public void Create_WithDetailsAtLimits_Succeeds()
    {
        var details = new ProductDetails(new string('N', Product.NameMaximumLength), new string('D', Product.DescriptionMaximumLength), 0);
        var product = Product.Create(CreateActiveCategory(10), ValidSku, details, CreationTime);
        product.Name.ShouldBe(details.Name);
        product.Description.ShouldBe(details.Description);
        product.MinimumStock.ShouldBe(0);
    }

    [Fact]
    public void Create_WithoutDescription_Succeeds()
    {
        var details = new ProductDetails("Abc", null, 0);
        var product = Product.Create(CreateActiveCategory(10), ValidSku, details, CreationTime);
        product.Description.ShouldBeNull();
    }

    [Theory]
    [MemberData(nameof(InvalidDetails))]
    public void Create_WithInvalidDetails_ThrowsValidationError(ProductDetails details)
    {
        var category = CreateActiveCategory(10);
        var exception = Should.Throw<DomainException>(() => Product.Create(category, ValidSku, details, CreationTime));
        exception.Code.ShouldBe(ErrorCode.ValidationError);
    }

    [Fact]
    public void Create_WithInactiveCategory_ThrowsReferencedCategoryInvalidWithCategoryId()
    {
        var category = CreateInactiveCategory(10);
        var exception = Should.Throw<DomainException>(() => Product.Create(category, ValidSku, ValidDetails, CreationTime));
        exception.Code.ShouldBe(ErrorCode.ReferencedCategoryInvalid);
        exception.MessageArguments.ShouldBe([10]);
    }

    [Fact]
    public void UpdateDetails_WhenActive_ChangesOnlyEditableFieldsAndUpdatedAt()
    {
        var product = CreatePersistedProduct();
        var details = new ProductDetails("Skimmed milk", null, 12);
        product.UpdateDetails(details, UpdateTime);
        product.Name.ShouldBe("Skimmed milk");
        product.Description.ShouldBeNull();
        product.MinimumStock.ShouldBe(12);
        product.UpdatedAt.ShouldBe(UpdateTime);
        product.Sku.ShouldBe(ValidSku);
        product.CategoryId.ShouldBe(10);
        product.Stock.ShouldBe(0);
    }

    [Theory]
    [MemberData(nameof(InvalidDetails))]
    public void UpdateDetails_WithInvalidDetails_ThrowsValidationError(ProductDetails details)
    {
        var product = CreatePersistedProduct();
        var exception = Should.Throw<DomainException>(() => product.UpdateDetails(details, UpdateTime));
        exception.Code.ShouldBe(ErrorCode.ValidationError);
    }

    [Fact]
    public void UpdateDetails_WithInvalidDetails_KeepsPreviousState()
    {
        var product = CreatePersistedProduct();
        Should.Throw<DomainException>(() => product.UpdateDetails(new ProductDetails("No", null, 1), UpdateTime));
        product.Name.ShouldBe(ValidDetails.Name);
        product.MinimumStock.ShouldBe(ValidDetails.MinimumStock);
        product.UpdatedAt.ShouldBeNull();
    }

    [Fact]
    public void UpdateDetails_WhenInactive_ThrowsProductInactiveWithProductId()
    {
        var product = CreateInactiveProduct();
        var exception = Should.Throw<DomainException>(() => product.UpdateDetails(ValidDetails, UpdateTime));
        exception.Code.ShouldBe(ErrorCode.ProductInactive);
        exception.MessageArguments.ShouldBe([ProductId]);
    }

    [Fact]
    public void ChangeCategory_WithActiveCategory_ChangesCategoryAndUpdatedAt()
    {
        var product = CreatePersistedProduct();
        product.ChangeCategory(CreateActiveCategory(20), UpdateTime);
        product.CategoryId.ShouldBe(20);
        product.UpdatedAt.ShouldBe(UpdateTime);
    }

    [Fact]
    public void ChangeCategory_WithInactiveCategory_ThrowsReferencedCategoryInvalidWithCategoryId()
    {
        var product = CreatePersistedProduct();
        var category = CreateInactiveCategory(20);
        var exception = Should.Throw<DomainException>(() => product.ChangeCategory(category, UpdateTime));
        exception.Code.ShouldBe(ErrorCode.ReferencedCategoryInvalid);
        exception.MessageArguments.ShouldBe([20]);
    }

    [Fact]
    public void ChangeCategory_WithInactiveCategory_KeepsPreviousCategory()
    {
        var product = CreatePersistedProduct();
        Should.Throw<DomainException>(() => product.ChangeCategory(CreateInactiveCategory(20), UpdateTime));
        product.CategoryId.ShouldBe(10);
        product.UpdatedAt.ShouldBeNull();
    }

    [Fact]
    public void ChangeCategory_WhenInactive_ThrowsProductInactiveWithProductId()
    {
        var product = CreateInactiveProduct();
        var category = CreateActiveCategory(20);
        var exception = Should.Throw<DomainException>(() => product.ChangeCategory(category, UpdateTime));
        exception.Code.ShouldBe(ErrorCode.ProductInactive);
        exception.MessageArguments.ShouldBe([ProductId]);
    }

    [Fact]
    public void Delete_WithZeroStock_DeactivatesWithDiscontinuedReason()
    {
        var product = CreatePersistedProduct();
        product.Delete(UpdateTime, UserId);
        product.IsActive.ShouldBeFalse();
        product.DeactivationReason.ShouldBe(DeactivationReason.Discontinued);
        product.DeactivationNotes.ShouldBeNull();
        product.DeactivatedAt.ShouldBe(UpdateTime);
        product.DeactivatedBy.ShouldBe(UserId);
    }

    [Fact]
    public void Delete_WithStock_ThrowsProductHasStockWithStock()
    {
        var product = WithStock(CreatePersistedProduct(), 7);
        var exception = Should.Throw<DomainException>(() => product.Delete(UpdateTime, UserId));
        exception.Code.ShouldBe(ErrorCode.ProductHasStock);
        exception.MessageArguments.ShouldBe([7]);
    }

    [Fact]
    public void Delete_WithStock_KeepsProductActive()
    {
        var product = WithStock(CreatePersistedProduct(), 7);
        Should.Throw<DomainException>(() => product.Delete(UpdateTime, UserId));
        product.IsActive.ShouldBeTrue();
        product.DeactivatedAt.ShouldBeNull();
    }

    [Fact]
    public void Delete_WhenInactive_ThrowsProductInactiveWithProductId()
    {
        var product = CreateInactiveProduct();
        var exception = Should.Throw<DomainException>(() => product.Delete(UpdateTime, UserId));
        exception.Code.ShouldBe(ErrorCode.ProductInactive);
        exception.MessageArguments.ShouldBe([ProductId]);
    }

    [Fact]
    public void Deactivate_WithValidDeactivation_RecordsDeactivationAndFreezesStock()
    {
        var product = WithStock(CreatePersistedProduct(), 7);
        var deactivation = new ProductDeactivation(DeactivationReason.Damaged, "Broken cartons", UpdateTime, UserId);
        product.Deactivate(deactivation);
        product.IsActive.ShouldBeFalse();
        product.Stock.ShouldBe(7);
        product.DeactivationReason.ShouldBe(DeactivationReason.Damaged);
        product.DeactivationNotes.ShouldBe("Broken cartons");
        product.DeactivatedAt.ShouldBe(UpdateTime);
        product.DeactivatedBy.ShouldBe(UserId);
    }

    [Theory]
    [InlineData(DeactivationReason.Expired, null)]
    [InlineData(DeactivationReason.Other, "Supplier closed")]
    public void Deactivate_WithReasonAndAllowedNotes_Succeeds(DeactivationReason reason, string? notes)
    {
        var product = CreatePersistedProduct();
        product.Deactivate(new ProductDeactivation(reason, notes, UpdateTime, UserId));
        product.IsActive.ShouldBeFalse();
        product.DeactivationReason.ShouldBe(reason);
    }

    [Fact]
    public void Deactivate_WithNotesAtMaximumLength_Succeeds()
    {
        var product = CreatePersistedProduct();
        var notes = new string('N', Product.DeactivationNotesMaximumLength);
        product.Deactivate(new ProductDeactivation(DeactivationReason.Recalled, notes, UpdateTime, UserId));
        product.DeactivationNotes.ShouldBe(notes);
    }

    [Theory]
    [MemberData(nameof(InvalidDeactivations))]
    public void Deactivate_WithInvalidDeactivation_ThrowsValidationError(ProductDeactivation deactivation)
    {
        var product = CreatePersistedProduct();
        var exception = Should.Throw<DomainException>(() => product.Deactivate(deactivation));
        exception.Code.ShouldBe(ErrorCode.ValidationError);
    }

    [Fact]
    public void Deactivate_WhenReasonIsOtherWithoutNotes_KeepsProductActive()
    {
        var product = CreatePersistedProduct();
        var deactivation = new ProductDeactivation(DeactivationReason.Other, null, UpdateTime, UserId);
        Should.Throw<DomainException>(() => product.Deactivate(deactivation));
        product.IsActive.ShouldBeTrue();
        product.DeactivationReason.ShouldBeNull();
    }

    [Fact]
    public void Deactivate_WhenInactive_ThrowsProductInactiveWithProductId()
    {
        var product = CreateInactiveProduct();
        var deactivation = new ProductDeactivation(DeactivationReason.Expired, null, UpdateTime, UserId);
        var exception = Should.Throw<DomainException>(() => product.Deactivate(deactivation));
        exception.Code.ShouldBe(ErrorCode.ProductInactive);
        exception.MessageArguments.ShouldBe([ProductId]);
    }

    [Fact]
    public void Reactivate_WhenInactiveWithActiveCategory_ReactivatesAndRecordsReactivation()
    {
        var product = WithStock(CreatePersistedProduct(), 7);
        product.Deactivate(new ProductDeactivation(DeactivationReason.Recalled, null, UpdateTime, UserId));
        var reactivationTime = UpdateTime.AddDays(1);
        product.Reactivate(CreateActiveCategory(10), reactivationTime, "supervisor-sub");
        product.IsActive.ShouldBeTrue();
        product.Stock.ShouldBe(7);
        product.ReactivatedAt.ShouldBe(reactivationTime);
        product.ReactivatedBy.ShouldBe("supervisor-sub");
    }

    [Fact]
    public void Reactivate_WhenInactive_KeepsPreviousDeactivationData()
    {
        var product = CreateInactiveProduct();
        product.Reactivate(CreateActiveCategory(10), UpdateTime.AddDays(1), "supervisor-sub");
        product.DeactivationReason.ShouldBe(DeactivationReason.Expired);
        product.DeactivatedAt.ShouldBe(UpdateTime);
        product.DeactivatedBy.ShouldBe(UserId);
    }

    [Fact]
    public void Reactivate_WhenActive_ThrowsProductAlreadyActiveWithProductId()
    {
        var product = CreatePersistedProduct();
        var category = CreateActiveCategory(10);
        var exception = Should.Throw<DomainException>(() => product.Reactivate(category, UpdateTime, UserId));
        exception.Code.ShouldBe(ErrorCode.ProductAlreadyActive);
        exception.MessageArguments.ShouldBe([ProductId]);
    }

    [Fact]
    public void Reactivate_WithInactiveCategory_ThrowsCategoryInactiveWithCategoryId()
    {
        var product = CreateInactiveProduct();
        var category = CreateInactiveCategory(10);
        var exception = Should.Throw<DomainException>(() => product.Reactivate(category, UpdateTime, UserId));
        exception.Code.ShouldBe(ErrorCode.CategoryInactive);
        exception.MessageArguments.ShouldBe([10]);
    }

    [Fact]
    public void Reactivate_WithInactiveCategory_KeepsProductInactive()
    {
        var product = CreateInactiveProduct();
        Should.Throw<DomainException>(() => product.Reactivate(CreateInactiveCategory(10), UpdateTime, UserId));
        product.IsActive.ShouldBeFalse();
        product.ReactivatedAt.ShouldBeNull();
    }

    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(4, 5, true)]
    [InlineData(5, 5, true)]
    [InlineData(6, 5, false)]
    public void IsLowStock_ComparesStockWithMinimumStock(int stock, int minimumStock, bool expected)
    {
        var details = new ProductDetails(ValidDetails.Name, null, minimumStock);
        var product = WithStock(Product.Create(CreateActiveCategory(10), ValidSku, details, CreationTime), stock);
        product.IsLowStock().ShouldBe(expected);
    }

    private static Category CreateActiveCategory(int id)
    {
        var area = PersistedIdentity.Assign(Area.Create("FOOD", "Food", CreationTime), 1);

        return PersistedIdentity.Assign(Category.Create(area, "Dairy", CreationTime), id);
    }

    private static Category CreateInactiveCategory(int id)
    {
        var category = CreateActiveCategory(id);
        category.Deactivate(0);

        return category;
    }

    private static Product CreatePersistedProduct()
    {
        return PersistedIdentity.Assign(Product.Create(CreateActiveCategory(10), ValidSku, ValidDetails, CreationTime), ProductId);
    }

    private static Product CreateInactiveProduct()
    {
        var product = CreatePersistedProduct();
        product.Deactivate(new ProductDeactivation(DeactivationReason.Expired, null, UpdateTime, UserId));

        return product;
    }

    private static Product WithStock(Product product, int stock)
    {
        typeof(Product).GetProperty(nameof(Product.Stock))!.SetValue(product, stock);

        return product;
    }
}
