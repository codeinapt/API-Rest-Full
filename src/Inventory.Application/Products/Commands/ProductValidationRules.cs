using FluentValidation;
using Inventory.Application.Common.Messages;
using Inventory.Domain.Products;

namespace Inventory.Application.Products.Commands;

public static class ProductValidationRules
{
    public static IRuleBuilderOptions<T, string> MustBeValidSku<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty().WithMessage(ErrorMessages.ProductSkuInvalid)
            .Length(Product.SkuMinimumLength, Product.SkuMaximumLength).WithMessage(ErrorMessages.ProductSkuInvalid)
            .Matches(Product.SkuPattern).WithMessage(ErrorMessages.ProductSkuInvalid);
    }

    public static IRuleBuilderOptions<T, string> MustBeValidProductName<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty().WithMessage(ErrorMessages.ProductNameInvalid)
            .Length(Product.NameMinimumLength, Product.NameMaximumLength).WithMessage(ErrorMessages.ProductNameInvalid);
    }

    public static IRuleBuilderOptions<T, string?> MustBeValidProductDescription<T>(this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .MaximumLength(Product.DescriptionMaximumLength).WithMessage(ErrorMessages.ProductDescriptionInvalid);
    }

    public static IRuleBuilderOptions<T, int> MustBeValidMinimumStock<T>(this IRuleBuilder<T, int> ruleBuilder)
    {
        return ruleBuilder
            .GreaterThanOrEqualTo(0).WithMessage(ErrorMessages.MinimumStockInvalid);
    }
}
