using FluentValidation;
using Inventory.Application.Common.Messages;
using Inventory.Application.Common.Pagination;

namespace Inventory.Application.Categories.Queries.GetCategories;

public sealed class GetCategoriesValidator : AbstractValidator<GetCategoriesQuery>
{
    public GetCategoriesValidator()
    {
        RuleFor(query => query.Parameters)
            .SetValidator(new PaginationParametersValidator())
            .OverridePropertyName(string.Empty);
        RuleFor(query => query.Parameters.Status)
            .IsInEnum()
            .WithMessage(ErrorMessages.StatusInvalid)
            .OverridePropertyName(nameof(CategoryQueryParameters.Status));
    }
}
