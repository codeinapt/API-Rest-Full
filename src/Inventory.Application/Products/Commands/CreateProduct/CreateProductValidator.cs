using FluentValidation;
using Inventory.Application.Products.Dtos;

namespace Inventory.Application.Products.Commands.CreateProduct;

public sealed class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator()
    {
        RuleFor(command => command.Request.Sku)
            .Cascade(CascadeMode.Stop)
            .MustBeValidSku()
            .OverridePropertyName(nameof(CreateProductRequest.Sku));
        RuleFor(command => command.Request.Name)
            .Cascade(CascadeMode.Stop)
            .MustBeValidProductName()
            .OverridePropertyName(nameof(CreateProductRequest.Name));
        RuleFor(command => command.Request.Description)
            .MustBeValidProductDescription()
            .OverridePropertyName(nameof(CreateProductRequest.Description));
        RuleFor(command => command.Request.MinimumStock)
            .MustBeValidMinimumStock()
            .OverridePropertyName(nameof(CreateProductRequest.MinimumStock));
    }
}
