using FluentValidation;
using Inventory.Application.Common.Messages;
using Inventory.Domain.Categories;

namespace Inventory.Application.Categories.Commands;

public static class CategoryValidationRules
{
    public static IRuleBuilderOptions<T, string> MustBeValidCategoryName<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty().WithMessage(ErrorMessages.CategoryNameInvalid)
            .Length(Category.NameMinimumLength, Category.NameMaximumLength).WithMessage(ErrorMessages.CategoryNameInvalid);
    }
}
