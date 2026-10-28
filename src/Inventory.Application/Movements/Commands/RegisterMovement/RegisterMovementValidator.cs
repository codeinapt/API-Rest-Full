using FluentValidation;
using Inventory.Application.Common.Messages;
using Inventory.Application.Movements.Dtos;
using Inventory.Domain.Movements;

namespace Inventory.Application.Movements.Commands.RegisterMovement;

public sealed class RegisterMovementValidator : AbstractValidator<RegisterMovementCommand>
{
    public RegisterMovementValidator()
    {
        RuleFor(command => command.Request.Type)
            .Must(type => type is MovementType.In or MovementType.Out).WithMessage(ErrorMessages.MovementTypeInvalid)
            .OverridePropertyName(nameof(RegisterMovementRequest.Type));
        RuleFor(command => command.Request.Quantity)
            .InclusiveBetween(1, InventoryMovement.MaximumQuantity).WithMessage(ErrorMessages.MovementQuantityInvalid)
            .OverridePropertyName(nameof(RegisterMovementRequest.Quantity));
        RuleFor(command => command.Request.Notes)
            .MaximumLength(InventoryMovement.NotesMaximumLength).WithMessage(ErrorMessages.MovementNotesTooLong)
            .OverridePropertyName(nameof(RegisterMovementRequest.Notes));
    }
}
