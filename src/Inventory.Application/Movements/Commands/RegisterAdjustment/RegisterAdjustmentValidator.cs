using FluentValidation;
using Inventory.Application.Common.Messages;
using Inventory.Application.Movements.Dtos;
using Inventory.Domain.Movements;

namespace Inventory.Application.Movements.Commands.RegisterAdjustment;

public sealed class RegisterAdjustmentValidator : AbstractValidator<RegisterAdjustmentCommand>
{
    public RegisterAdjustmentValidator()
    {
        RuleFor(command => command.Request.Delta)
            .Must(delta => delta is >= -InventoryMovement.MaximumQuantity and <= InventoryMovement.MaximumQuantity and not 0).WithMessage(ErrorMessages.AdjustmentDeltaInvalid)
            .OverridePropertyName(nameof(RegisterAdjustmentRequest.Delta));
        RuleFor(command => command.Request.Reason)
            .IsInEnum().WithMessage(ErrorMessages.AdjustmentReasonInvalid)
            .OverridePropertyName(nameof(RegisterAdjustmentRequest.Reason));
        RuleFor(command => command.Request.Notes)
            .MaximumLength(InventoryMovement.NotesMaximumLength).WithMessage(ErrorMessages.MovementNotesTooLong)
            .OverridePropertyName(nameof(RegisterAdjustmentRequest.Notes));
    }
}
