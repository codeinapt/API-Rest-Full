using FluentValidation;
using Inventory.Application.Common.Messages;
using Inventory.Application.Products.Dtos;
using Inventory.Domain.Products;

namespace Inventory.Application.Products.Commands.DeactivateProduct;

public sealed class DeactivateProductValidator : AbstractValidator<DeactivateProductCommand>
{
    public DeactivateProductValidator()
    {
        RuleFor(command => command.Request.Reason)
            .IsInEnum().WithMessage(ErrorMessages.DeactivationReasonInvalid)
            .OverridePropertyName(nameof(DeactivateProductRequest.Reason));
        RuleFor(command => command.Request.Notes)
            .MaximumLength(Product.DeactivationNotesMaximumLength).WithMessage(ErrorMessages.DeactivationNotesTooLong)
            .OverridePropertyName(nameof(DeactivateProductRequest.Notes));
        RuleFor(command => command.Request.Notes)
            .NotEmpty().WithMessage(ErrorMessages.DeactivationNotesRequired)
            .When(command => command.Request.Reason == DeactivationReason.Other)
            .OverridePropertyName(nameof(DeactivateProductRequest.Notes));
    }
}
