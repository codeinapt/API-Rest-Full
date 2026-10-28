using FluentValidation;
using Inventory.Application.Common.Messages;

namespace Inventory.Application.Common.Pagination;

public sealed class PaginationParametersValidator : AbstractValidator<PaginationParameters>
{
    public PaginationParametersValidator()
    {
        RuleFor(pagination => pagination.Page)
            .GreaterThanOrEqualTo(PaginationParameters.FirstPage)
            .WithMessage(ErrorMessages.PageOutOfRange);
        RuleFor(pagination => pagination.PageSize)
            .InclusiveBetween(PaginationParameters.MinimumPageSize, PaginationParameters.MaximumPageSize)
            .WithMessage(ErrorMessages.PageSizeOutOfRange);
    }
}
