using FluentValidation;
using Inventory.Application.Products.Dtos;

namespace Inventory.Application.Products.Commands.UpdateProduct;

public sealed class UpdateProductValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductValidator()
    {
        RuleFor(command => command.Request.Name)
            .Cascade(CascadeMode.Stop)
            .MustBeValidProductName()
            .OverridePropertyName(nameof(UpdateProductRequest.Name));
        RuleFor(command => command.Request.Description)
            .MustBeValidProductDescription()
            .OverridePropertyName(nameof(UpdateProductRequest.Description));
        RuleFor(command => command.Request.MinimumStock)
            .MustBeValidMinimumStock()
            .OverridePropertyName(nameof(UpdateProductRequest.MinimumStock));
    }
}
