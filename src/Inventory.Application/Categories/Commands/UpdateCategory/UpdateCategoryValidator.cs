using FluentValidation;
using Inventory.Application.Categories.Dtos;

namespace Inventory.Application.Categories.Commands.UpdateCategory;

public sealed class UpdateCategoryValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryValidator()
    {
        RuleFor(command => command.Request.Name)
            .Cascade(CascadeMode.Stop)
            .MustBeValidCategoryName()
            .OverridePropertyName(nameof(UpdateCategoryRequest.Name));
    }
}
