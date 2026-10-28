using Inventory.Application.Common.Messages;
using Inventory.Application.Products.Commands.UpdateProduct;
using Inventory.Application.Products.Dtos;
using Inventory.Domain.Products;

namespace Inventory.Application.Tests.Products.Commands.UpdateProduct;

public sealed class UpdateProductValidatorTests
{
    private const int ProductId = 21;
    private const string ValidName = "Whole milk";

    private readonly UpdateProductValidator validator = new();

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
    [InlineData("Abc", null, 0)]
    [InlineData("Whole milk", "One litre bottle", 5)]
    public void Validate_WithValidRequest_IsValid(string name, string? description, int minimumStock)
    {
        var result = validator.Validate(new UpdateProductCommand(ProductId, new UpdateProductRequest(name, description, minimumStock)));
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithFieldsAtMaximumLength_IsValid()
    {
        var name = new string('N', Product.NameMaximumLength);
        var description = new string('D', Product.DescriptionMaximumLength);
        var result = validator.Validate(new UpdateProductCommand(ProductId, new UpdateProductRequest(name, description, 0)));
        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [MemberData(nameof(InvalidNames))]
    public void Validate_WithInvalidName_FailsOnNameWithSpanishMessage(string name)
    {
        var result = validator.Validate(new UpdateProductCommand(ProductId, new UpdateProductRequest(name, null, 0)));
        result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            failure => failure.PropertyName.ShouldBe(nameof(UpdateProductRequest.Name)),
            failure => failure.ErrorMessage.ShouldBe(ErrorMessages.ProductNameInvalid));
    }

    [Fact]
    public void Validate_WithDescriptionOverMaximumLength_FailsOnDescriptionWithSpanishMessage()
    {
        var description = new string('D', Product.DescriptionMaximumLength + 1);
        var result = validator.Validate(new UpdateProductCommand(ProductId, new UpdateProductRequest(ValidName, description, 0)));
        result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            failure => failure.PropertyName.ShouldBe(nameof(UpdateProductRequest.Description)),
            failure => failure.ErrorMessage.ShouldBe(ErrorMessages.ProductDescriptionInvalid));
    }

    [Fact]
    public void Validate_WithNegativeMinimumStock_FailsOnMinimumStockWithSpanishMessage()
    {
        var result = validator.Validate(new UpdateProductCommand(ProductId, new UpdateProductRequest(ValidName, null, -1)));
        result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            failure => failure.PropertyName.ShouldBe(nameof(UpdateProductRequest.MinimumStock)),
            failure => failure.ErrorMessage.ShouldBe(ErrorMessages.MinimumStockInvalid));
    }
}
