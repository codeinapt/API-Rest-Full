using Inventory.Application.Areas.Queries.GetAreas;
using Inventory.Application.Common.Messages;
using Inventory.Application.Common.Pagination;

namespace Inventory.Application.Tests.Areas.Queries.GetAreas;

public sealed class GetAreasValidatorTests
{
    private readonly GetAreasValidator validator = new();

    [Theory]
    [InlineData(StatusFilter.Active)]
    [InlineData(StatusFilter.Inactive)]
    [InlineData(StatusFilter.All)]
    public void Validate_WhenParametersAreInRange_IsValid(StatusFilter status)
    {
        var result = validator.Validate(new GetAreasQuery(new AreaQueryParameters { Status = status }));
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WhenPageIsBelowOne_FailsOnPageWithoutParametersPrefix()
    {
        var result = validator.Validate(new GetAreasQuery(new AreaQueryParameters { Page = 0 }));
        result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            failure => failure.PropertyName.ShouldBe(nameof(AreaQueryParameters.Page)),
            failure => failure.ErrorMessage.ShouldBe(ErrorMessages.PageOutOfRange));
    }

    [Fact]
    public void Validate_WhenPageSizeIsAboveMaximum_FailsOnPageSizeWithoutParametersPrefix()
    {
        var result = validator.Validate(new GetAreasQuery(new AreaQueryParameters { PageSize = 101 }));
        result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            failure => failure.PropertyName.ShouldBe(nameof(AreaQueryParameters.PageSize)),
            failure => failure.ErrorMessage.ShouldBe(ErrorMessages.PageSizeOutOfRange));
    }

    [Fact]
    public void Validate_WhenStatusIsNotADefinedValue_FailsOnStatusWithSpanishMessage()
    {
        var result = validator.Validate(new GetAreasQuery(new AreaQueryParameters { Status = (StatusFilter)7 }));
        result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            failure => failure.PropertyName.ShouldBe(nameof(AreaQueryParameters.Status)),
            failure => failure.ErrorMessage.ShouldBe(ErrorMessages.StatusInvalid));
    }
}
