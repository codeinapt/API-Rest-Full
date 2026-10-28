using Inventory.Application.Common.Messages;
using Inventory.Application.Products.Commands.CreateProduct;
using Inventory.Application.Products.Dtos;
using Inventory.Domain.Products;

namespace Inventory.Application.Tests.Products.Commands.CreateProduct;

public sealed class CreateProductValidatorTests
{
    private const string ValidSku = "MILK-001";
    private const string ValidName = "Whole milk";
    private const int CategoryId = 7;

    private readonly CreateProductValidator validator = new();

    public static TheoryData<string> InvalidSkus()
    {
        return new TheoryData<string>
        {
            string.Empty,
            "AB",
            new string('S', Product.SkuMaximumLength + 1),
            "milk-001",
            "MILK 001",
            "MILK_001",
        };
    }

    public static TheoryData<string> InvalidNames()
    {
        return new TheoryData<string>
        {
            string.Empty,
            "   ",
            "No",
            new string('N', Product.NameMaximumLength + 1),
        };
    }

    [Theory]
    [InlineData("ABC", "Abc", null, 0)]
    [InlineData("MILK-001", "Whole milk", "One litre bottle", 5)]
    public void Validate_WithValidRequest_IsValid(string sku, string name, string? description, int minimumStock)
    {
        var result = validator.Validate(CreateCommand(new CreateProductRequest(sku, name, description, CategoryId, minimumStock)));
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithFieldsAtMaximumLength_IsValid()
    {
        var sku = new string('S', Product.SkuMaximumLength);
        var name = new string('N', Product.NameMaximumLength);
        var description = new string('D', Product.DescriptionMaximumLength);
        var result = validator.Validate(CreateCommand(new CreateProductRequest(sku, name, description, CategoryId, 0)));
        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [MemberData(nameof(InvalidSkus))]
    public void Validate_WithInvalidSku_FailsOnSkuWithSpanishMessage(string sku)
    {
        var result = validator.Validate(CreateCommand(new CreateProductRequest(sku, ValidName, null, CategoryId, 0)));
        result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            failure => failure.PropertyName.ShouldBe(nameof(CreateProductRequest.Sku)),
            failure => failure.ErrorMessage.ShouldBe(ErrorMessages.ProductSkuInvalid));
    }

    [Theory]
    [MemberData(nameof(InvalidNames))]
    public void Validate_WithInvalidName_FailsOnNameWithSpanishMessage(string name)
    {
        var result = validator.Validate(CreateCommand(new CreateProductRequest(ValidSku, name, null, CategoryId, 0)));
        result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            failure => failure.PropertyName.ShouldBe(nameof(CreateProductRequest.Name)),
            failure => failure.ErrorMessage.ShouldBe(ErrorMessages.ProductNameInvalid));
    }

    [Fact]
    public void Validate_WithDescriptionOverMaximumLength_FailsOnDescriptionWithSpanishMessage()
    {
        var description = new string('D', Product.DescriptionMaximumLength + 1);
        var result = validator.Validate(CreateCommand(new CreateProductRequest(ValidSku, ValidName, description, CategoryId, 0)));
        result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            failure => failure.PropertyName.ShouldBe(nameof(CreateProductRequest.Description)),
            failure => failure.ErrorMessage.ShouldBe(ErrorMessages.ProductDescriptionInvalid));
    }

    [Fact]
    public void Validate_WithNegativeMinimumStock_FailsOnMinimumStockWithSpanishMessage()
    {
        var result = validator.Validate(CreateCommand(new CreateProductRequest(ValidSku, ValidName, null, CategoryId, -1)));
        result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            failure => failure.PropertyName.ShouldBe(nameof(CreateProductRequest.MinimumStock)),
            failure => failure.ErrorMessage.ShouldBe(ErrorMessages.MinimumStockInvalid));
    }

    private static CreateProductCommand CreateCommand(CreateProductRequest request)
    {
        return new CreateProductCommand(request);
    }
}
