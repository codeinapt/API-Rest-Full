using Inventory.Application.Common.Messages;
using Inventory.Application.Movements.Queries.GetMovements;
using Inventory.Domain.Movements;

namespace Inventory.Application.Tests.Movements.Queries.GetMovements;

public sealed class GetMovementsValidatorTests
{
    private const int ProductId = 21;
    private static readonly DateTimeOffset October1st = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly GetMovementsValidator validator = new();

    [Theory]
    [InlineData(null)]
    [InlineData(MovementType.In)]
    [InlineData(MovementType.Out)]
    [InlineData(MovementType.Adjustment)]
    public void Validate_WhenTypeIsAbsentOrDefined_IsValid(MovementType? type)
    {
        var result = validator.Validate(new GetMovementsQuery(ProductId, new MovementQueryParameters { Type = type }));
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WhenFromEqualsTo_IsValid()
    {
        var result = validator.Validate(new GetMovementsQuery(ProductId, new MovementQueryParameters { From = October1st, To = October1st }));
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WhenOnlyOneDateBoundIsGiven_IsValid()
    {
        var onlyFrom = validator.Validate(new GetMovementsQuery(ProductId, new MovementQueryParameters { From = October1st }));
        var onlyTo = validator.Validate(new GetMovementsQuery(ProductId, new MovementQueryParameters { To = October1st }));
        onlyFrom.ShouldSatisfyAllConditions(
            () => onlyFrom.IsValid.ShouldBeTrue(),
            () => onlyTo.IsValid.ShouldBeTrue());
    }

    [Fact]
    public void Validate_WhenFromIsTheSameInstantAsToInAnotherOffset_IsValid()
    {
        var sameInstantInBogota = October1st.ToOffset(TimeSpan.FromHours(-5));
        var result = validator.Validate(new GetMovementsQuery(ProductId, new MovementQueryParameters { From = sameInstantInBogota, To = October1st }));
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WhenFromIsAfterTo_FailsOnFromWithSpanishMessage()
    {
        var parameters = new MovementQueryParameters { From = October1st.AddSeconds(1), To = October1st };
        var result = validator.Validate(new GetMovementsQuery(ProductId, parameters));
        result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            failure => failure.PropertyName.ShouldBe(nameof(MovementQueryParameters.From)),
            failure => failure.ErrorMessage.ShouldBe(ErrorMessages.MovementDateRangeInvalid));
    }

    [Fact]
    public void Validate_WhenTypeIsNotADefinedValue_FailsOnTypeWithSpanishMessage()
    {
        var result = validator.Validate(new GetMovementsQuery(ProductId, new MovementQueryParameters { Type = (MovementType)7 }));
        result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            failure => failure.PropertyName.ShouldBe(nameof(MovementQueryParameters.Type)),
            failure => failure.ErrorMessage.ShouldBe(ErrorMessages.MovementTypeFilterInvalid));
    }

    [Fact]
    public void Validate_WhenPageIsBelowOne_FailsOnPageWithoutParametersPrefix()
    {
        var result = validator.Validate(new GetMovementsQuery(ProductId, new MovementQueryParameters { Page = 0 }));
        result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            failure => failure.PropertyName.ShouldBe(nameof(MovementQueryParameters.Page)),
            failure => failure.ErrorMessage.ShouldBe(ErrorMessages.PageOutOfRange));
    }

    [Fact]
    public void Validate_WhenPageSizeIsAboveMaximum_FailsOnPageSizeWithoutParametersPrefix()
    {
        var result = validator.Validate(new GetMovementsQuery(ProductId, new MovementQueryParameters { PageSize = 101 }));
        result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            failure => failure.PropertyName.ShouldBe(nameof(MovementQueryParameters.PageSize)),
            failure => failure.ErrorMessage.ShouldBe(ErrorMessages.PageSizeOutOfRange));
    }
}
