using Inventory.Application.Common.Messages;
using Inventory.Application.Common.Pagination;

namespace Inventory.Application.Tests.Common.Pagination;

public sealed class PaginationParametersValidatorTests
{
    private readonly PaginationParametersValidator validator = new();

    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 20)]
    [InlineData(500, 100)]
    public void Validate_WhenPageAndPageSizeAreInRange_IsValid(int page, int pageSize)
    {
        var result = validator.Validate(new PaginationParameters { Page = page, PageSize = pageSize });
        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WhenPageIsBelowOne_FailsOnPageWithSpanishMessage(int page)
    {
        var result = validator.Validate(new PaginationParameters { Page = page });
        result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            failure => failure.PropertyName.ShouldBe(nameof(PaginationParameters.Page)),
            failure => failure.ErrorMessage.ShouldBe(ErrorMessages.PageOutOfRange));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Validate_WhenPageSizeIsOutOfRange_FailsOnPageSizeWithSpanishMessage(int pageSize)
    {
        var result = validator.Validate(new PaginationParameters { PageSize = pageSize });
        result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            failure => failure.PropertyName.ShouldBe(nameof(PaginationParameters.PageSize)),
            failure => failure.ErrorMessage.ShouldBe(ErrorMessages.PageSizeOutOfRange));
    }
}
