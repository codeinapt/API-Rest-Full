using FluentValidation;
using Inventory.Application.Common.Messages;
using Inventory.Application.Common.Pagination;

namespace Inventory.Application.Products.Queries.GetProducts;

public sealed class GetProductsValidator : AbstractValidator<GetProductsQuery>
{
    public GetProductsValidator()
    {
        RuleFor(query => query.Parameters)
            .SetValidator(new PaginationParametersValidator())
            .OverridePropertyName(string.Empty);
        RuleFor(query => query.Parameters.Status)
            .IsInEnum()
            .WithMessage(ErrorMessages.StatusInvalid)
            .OverridePropertyName(nameof(ProductQueryParameters.Status));
        RuleFor(query => query.Parameters.SortBy)
            .IsInEnum()
            .WithMessage(ErrorMessages.SortByInvalid)
            .OverridePropertyName(nameof(ProductQueryParameters.SortBy));
        RuleFor(query => query.Parameters.SortDirection)
            .IsInEnum()
            .WithMessage(ErrorMessages.SortDirectionInvalid)
            .OverridePropertyName(nameof(ProductQueryParameters.SortDirection));
    }
}
