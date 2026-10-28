using FluentValidation;
using Inventory.Application.Categories.Dtos;

namespace Inventory.Application.Categories.Commands.CreateCategory;

public sealed class CreateCategoryValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryValidator()
    {
        RuleFor(command => command.Request.Name)
            .Cascade(CascadeMode.Stop)
            .MustBeValidCategoryName()
            .OverridePropertyName(nameof(CreateCategoryRequest.Name));
    }
}
