using Inventory.Application.Categories.Commands.UpdateCategory;
using Inventory.Application.Categories.Dtos;
using Inventory.Application.Common.Messages;
using Inventory.Domain.Categories;

namespace Inventory.Application.Tests.Categories.Commands.UpdateCategory;

public sealed class UpdateCategoryValidatorTests
{
    private const int CategoryId = 15;
    private const int AreaId = 3;

    private readonly UpdateCategoryValidator validator = new();

    public static TheoryData<string> InvalidNames()
    {
        return new TheoryData<string>
        {
            string.Empty,
            "   ",
            "No",
            new string('N', Category.NameMaximumLength + 1),
        };
    }

    [Theory]
    [InlineData("Abc")]
    [InlineData("Dairy products")]
    public void Validate_WithValidName_IsValid(string name)
    {
        var result = validator.Validate(new UpdateCategoryCommand(CategoryId, new UpdateCategoryRequest(AreaId, name)));
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithNameAtMaximumLength_IsValid()
    {
        var name = new string('N', Category.NameMaximumLength);
        var result = validator.Validate(new UpdateCategoryCommand(CategoryId, new UpdateCategoryRequest(AreaId, name)));
        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [MemberData(nameof(InvalidNames))]
    public void Validate_WithInvalidName_FailsOnNameWithSpanishMessage(string name)
    {
        var result = validator.Validate(new UpdateCategoryCommand(CategoryId, new UpdateCategoryRequest(AreaId, name)));
        result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            failure => failure.PropertyName.ShouldBe(nameof(UpdateCategoryRequest.Name)),
            failure => failure.ErrorMessage.ShouldBe(ErrorMessages.CategoryNameInvalid));
    }
}
