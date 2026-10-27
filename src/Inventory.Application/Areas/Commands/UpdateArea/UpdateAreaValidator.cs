using FluentValidation;
using Inventory.Application.Areas.Dtos;

namespace Inventory.Application.Areas.Commands.UpdateArea;

public sealed class UpdateAreaValidator : AbstractValidator<UpdateAreaCommand>
{
    public UpdateAreaValidator()
    {
        RuleFor(command => command.Request.Name)
            .Cascade(CascadeMode.Stop)
            .MustBeValidAreaName()
            .OverridePropertyName(nameof(UpdateAreaRequest.Name));
    }
}
