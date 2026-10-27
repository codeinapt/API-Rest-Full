using FluentValidation;
using Inventory.Application.Areas.Dtos;

namespace Inventory.Application.Areas.Commands.CreateArea;

public sealed class CreateAreaValidator : AbstractValidator<CreateAreaCommand>
{
    public CreateAreaValidator()
    {
        RuleFor(command => command.Request.Code)
            .Cascade(CascadeMode.Stop)
            .MustBeValidAreaCode()
            .OverridePropertyName(nameof(CreateAreaRequest.Code));
        RuleFor(command => command.Request.Name)
            .Cascade(CascadeMode.Stop)
            .MustBeValidAreaName()
            .OverridePropertyName(nameof(CreateAreaRequest.Name));
    }
}
