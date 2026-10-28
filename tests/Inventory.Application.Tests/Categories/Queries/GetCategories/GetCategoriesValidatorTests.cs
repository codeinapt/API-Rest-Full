using Inventory.Application.Categories.Queries.GetCategories;
using Inventory.Application.Common.Messages;
using Inventory.Application.Common.Pagination;

namespace Inventory.Application.Tests.Categories.Queries.GetCategories;

public sealed class GetCategoriesValidatorTests
{
    private readonly GetCategoriesValidator validator = new();

    [Theory]
    [InlineData(StatusFilter.Active)]
    [InlineData(StatusFilter.Inactive)]
    [InlineData(StatusFilter.All)]
    public void Validate_WhenParametersAreInRange_IsValid(StatusFilter status)
    {
        var result = validator.Validate(new GetCategoriesQuery(new CategoryQueryParameters { AreaId = 7, Status = status }));
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WhenPageIsBelowOne_FailsOnPageWithoutParametersPrefix()
    {
        var result = validator.Validate(new GetCategoriesQuery(new CategoryQueryParameters { Page = 0 }));
        result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            failure => failure.PropertyName.ShouldBe(nameof(CategoryQueryParameters.Page)),
            failure => failure.ErrorMessage.ShouldBe(ErrorMessages.PageOutOfRange));
    }

    [Fact]
    public void Validate_WhenPageSizeIsAboveMaximum_FailsOnPageSizeWithoutParametersPrefix()
    {
        var result = validator.Validate(new GetCategoriesQuery(new CategoryQueryParameters { PageSize = 101 }));
        result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            failure => failure.PropertyName.ShouldBe(nameof(CategoryQueryParameters.PageSize)),
            failure => failure.ErrorMessage.ShouldBe(ErrorMessages.PageSizeOutOfRange));
    }

    [Fact]
    public void Validate_WhenStatusIsNotADefinedValue_FailsOnStatusWithSpanishMessage()
    {
        var result = validator.Validate(new GetCategoriesQuery(new CategoryQueryParameters { Status = (StatusFilter)7 }));
        result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            failure => failure.PropertyName.ShouldBe(nameof(CategoryQueryParameters.Status)),
            failure => failure.ErrorMessage.ShouldBe(ErrorMessages.StatusInvalid));
    }
}
