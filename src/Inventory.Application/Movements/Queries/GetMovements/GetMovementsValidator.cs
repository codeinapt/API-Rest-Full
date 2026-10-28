using FluentValidation;
using Inventory.Application.Common.Messages;
using Inventory.Application.Common.Pagination;

namespace Inventory.Application.Movements.Queries.GetMovements;

public sealed class GetMovementsValidator : AbstractValidator<GetMovementsQuery>
{
    public GetMovementsValidator()
    {
        RuleFor(query => query.Parameters)
            .SetValidator(new PaginationParametersValidator())
            .OverridePropertyName(string.Empty);
        RuleFor(query => query.Parameters.Type)
            .IsInEnum()
            .WithMessage(ErrorMessages.MovementTypeFilterInvalid)
            .OverridePropertyName(nameof(MovementQueryParameters.Type));
        RuleFor(query => query.Parameters)
            .Must(HaveChronologicalDateRange)
            .WithMessage(ErrorMessages.MovementDateRangeInvalid)
            .OverridePropertyName(nameof(MovementQueryParameters.From));
    }

    private static bool HaveChronologicalDateRange(MovementQueryParameters parameters)
    {
        return parameters.From is null || parameters.To is null || parameters.From <= parameters.To;
    }
}
