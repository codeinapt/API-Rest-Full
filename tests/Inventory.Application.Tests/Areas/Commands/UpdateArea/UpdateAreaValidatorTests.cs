using Inventory.Application.Areas.Commands.UpdateArea;
using Inventory.Application.Areas.Dtos;
using Inventory.Application.Common.Messages;
using Inventory.Domain.Areas;

namespace Inventory.Application.Tests.Areas.Commands.UpdateArea;

public sealed class UpdateAreaValidatorTests
{
    private const int AreaId = 7;

    private readonly UpdateAreaValidator validator = new();

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
    [InlineData("Abc")]
    [InlineData("Main storage")]
    public void Validate_WithValidName_IsValid(string name)
    {
        var result = validator.Validate(new UpdateAreaCommand(AreaId, new UpdateAreaRequest(name)));
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithNameAtMaximumLength_IsValid()
    {
        var name = new string('N', Area.NameMaximumLength);
        var result = validator.Validate(new UpdateAreaCommand(AreaId, new UpdateAreaRequest(name)));
        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [MemberData(nameof(InvalidNames))]
    public void Validate_WithInvalidName_FailsOnNameWithSpanishMessage(string name)
    {
        var result = validator.Validate(new UpdateAreaCommand(AreaId, new UpdateAreaRequest(name)));
        result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            failure => failure.PropertyName.ShouldBe(nameof(UpdateAreaRequest.Name)),
            failure => failure.ErrorMessage.ShouldBe(ErrorMessages.AreaNameInvalid));
    }
}
