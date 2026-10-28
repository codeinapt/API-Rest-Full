using Inventory.Application.Areas.Commands.CreateArea;
using Inventory.Application.Areas.Dtos;
using Inventory.Application.Common.Messages;
using Inventory.Domain.Areas;

namespace Inventory.Application.Tests.Areas.Commands.CreateArea;

public sealed class CreateAreaValidatorTests
{
    private const string ValidCode = "WH";
    private const string ValidName = "Warehouse";

    private readonly CreateAreaValidator validator = new();

    public static TheoryData<string> InvalidNames()
    {
        return new TheoryData<string>
        {
            string.Empty,
            "   ",
            "No",
            new string('N', Area.NameMaximumLength + 1),
        };
    }

    [Theory]
    [InlineData("AB", "Abc")]
    [InlineData("ABCDEFGHIJ", "Main storage")]
    public void Validate_WithValidCodeAndName_IsValid(string code, string name)
    {
        var result = validator.Validate(new CreateAreaCommand(new CreateAreaRequest(code, name)));
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithNameAtMaximumLength_IsValid()
    {
        var name = new string('N', Area.NameMaximumLength);
        var result = validator.Validate(new CreateAreaCommand(new CreateAreaRequest(ValidCode, name)));
        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("ABCDEFGHIJK")]
    [InlineData("wh")]
    [InlineData("W1")]
    [InlineData("W-H")]
    [InlineData("ÄB")]
    [InlineData("WH\n")]
    public void Validate_WithInvalidCode_FailsOnCodeWithSpanishMessage(string code)
    {
        var result = validator.Validate(new CreateAreaCommand(new CreateAreaRequest(code, ValidName)));
        result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            failure => failure.PropertyName.ShouldBe(nameof(CreateAreaRequest.Code)),
            failure => failure.ErrorMessage.ShouldBe(ErrorMessages.AreaCodeInvalid));
    }

    [Theory]
    [MemberData(nameof(InvalidNames))]
    public void Validate_WithInvalidName_FailsOnNameWithSpanishMessage(string name)
    {
        var result = validator.Validate(new CreateAreaCommand(new CreateAreaRequest(ValidCode, name)));
        result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            failure => failure.PropertyName.ShouldBe(nameof(CreateAreaRequest.Name)),
            failure => failure.ErrorMessage.ShouldBe(ErrorMessages.AreaNameInvalid));
    }
}
