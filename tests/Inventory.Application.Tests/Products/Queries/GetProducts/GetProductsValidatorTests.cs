using Inventory.Application.Common.Messages;
using Inventory.Application.Common.Pagination;
using Inventory.Application.Products.Queries.GetProducts;

namespace Inventory.Application.Tests.Products.Queries.GetProducts;

public sealed class GetProductsValidatorTests
{
    private readonly GetProductsValidator validator = new();

    [Theory]
    [InlineData(StatusFilter.Active, ProductSortField.Name, SortDirection.Asc)]
    [InlineData(StatusFilter.Inactive, ProductSortField.Sku, SortDirection.Desc)]
    [InlineData(StatusFilter.All, ProductSortField.Stock, SortDirection.Asc)]
    public void Validate_WhenParametersAreAllowed_IsValid(StatusFilter status, ProductSortField sortBy, SortDirection sortDirection)
    {
        var parameters = new ProductQueryParameters { CategoryId = 3, Status = status, Search = "drill", LowStock = true, SortBy = sortBy, SortDirection = sortDirection };
        var result = validator.Validate(new GetProductsQuery(parameters));
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WhenPageIsBelowOne_FailsOnPageWithoutParametersPrefix()
    {
        var result = validator.Validate(new GetProductsQuery(new ProductQueryParameters { Page = 0 }));
        result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            failure => failure.PropertyName.ShouldBe(nameof(ProductQueryParameters.Page)),
            failure => failure.ErrorMessage.ShouldBe(ErrorMessages.PageOutOfRange));
    }

    [Fact]
    public void Validate_WhenPageSizeIsAboveMaximum_FailsOnPageSizeWithoutParametersPrefix()
    {
        var result = validator.Validate(new GetProductsQuery(new ProductQueryParameters { PageSize = 101 }));
        result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            failure => failure.PropertyName.ShouldBe(nameof(ProductQueryParameters.PageSize)),
            failure => failure.ErrorMessage.ShouldBe(ErrorMessages.PageSizeOutOfRange));
    }

    [Fact]
    public void Validate_WhenStatusIsNotADefinedValue_FailsOnStatusWithSpanishMessage()
    {
        var result = validator.Validate(new GetProductsQuery(new ProductQueryParameters { Status = (StatusFilter)7 }));
        result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            failure => failure.PropertyName.ShouldBe(nameof(ProductQueryParameters.Status)),
            failure => failure.ErrorMessage.ShouldBe(ErrorMessages.StatusInvalid));
    }

    [Fact]
    public void Validate_WhenSortByIsNotInTheAllowedList_FailsOnSortByWithSpanishMessage()
    {
        var result = validator.Validate(new GetProductsQuery(new ProductQueryParameters { SortBy = (ProductSortField)7 }));
        result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            failure => failure.PropertyName.ShouldBe(nameof(ProductQueryParameters.SortBy)),
            failure => failure.ErrorMessage.ShouldBe(ErrorMessages.SortByInvalid));
    }

    [Fact]
    public void Validate_WhenSortDirectionIsNotADefinedValue_FailsOnSortDirectionWithSpanishMessage()
    {
        var result = validator.Validate(new GetProductsQuery(new ProductQueryParameters { SortDirection = (SortDirection)7 }));
        result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            failure => failure.PropertyName.ShouldBe(nameof(ProductQueryParameters.SortDirection)),
            failure => failure.ErrorMessage.ShouldBe(ErrorMessages.SortDirectionInvalid));
    }
}
