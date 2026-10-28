using FluentValidation;
using Inventory.Application.Common.Messages;
using Inventory.Application.Common.Pagination;

namespace Inventory.Application.Areas.Queries.GetAreas;

public sealed class GetAreasValidator : AbstractValidator<GetAreasQuery>
{
    public GetAreasValidator()
    {
        RuleFor(query => query.Parameters)
            .SetValidator(new PaginationParametersValidator())
            .OverridePropertyName(string.Empty);
        RuleFor(query => query.Parameters.Status)
            .IsInEnum()
            .WithMessage(ErrorMessages.StatusInvalid)
            .OverridePropertyName(nameof(AreaQueryParameters.Status));
    }
}
