using FluentValidation;
using Inventory.Application.Common.Messages;
using Inventory.Domain.Areas;

namespace Inventory.Application.Areas.Commands;

public static class AreaValidationRules
{
    public static IRuleBuilderOptions<T, string> MustBeValidAreaCode<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty().WithMessage(ErrorMessages.AreaCodeInvalid)
            .Length(Area.CodeMinimumLength, Area.CodeMaximumLength).WithMessage(ErrorMessages.AreaCodeInvalid)
            .Matches(Area.CodePattern).WithMessage(ErrorMessages.AreaCodeInvalid);
    }

    public static IRuleBuilderOptions<T, string> MustBeValidAreaName<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty().WithMessage(ErrorMessages.AreaNameInvalid)
            .Length(Area.NameMinimumLength, Area.NameMaximumLength).WithMessage(ErrorMessages.AreaNameInvalid);
    }
}
