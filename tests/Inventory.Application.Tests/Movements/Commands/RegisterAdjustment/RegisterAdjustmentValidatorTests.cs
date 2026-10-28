using Inventory.Application.Common.Messages;
using Inventory.Application.Movements.Commands.RegisterAdjustment;
using Inventory.Application.Movements.Dtos;
using Inventory.Domain.Movements;

namespace Inventory.Application.Tests.Movements.Commands.RegisterAdjustment;

public sealed class RegisterAdjustmentValidatorTests
{
    private const int ProductId = 21;

    private readonly RegisterAdjustmentValidator validator = new();

    [Theory]
    [InlineData(1, AdjustmentReason.Found, null)]
    [InlineData(-1, AdjustmentReason.Damage, "")]
    [InlineData(InventoryMovement.MaximumQuantity, AdjustmentReason.PhysicalCountCorrection, "Yearly count")]
    [InlineData(-InventoryMovement.MaximumQuantity, AdjustmentReason.LossOrTheft, "Warehouse theft")]
    public void Validate_WithValidRequest_IsValid(int delta, AdjustmentReason reason, string? notes)
    {
        var result = validator.Validate(CreateCommand(delta, reason, notes));
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithNotesAtMaximumLength_IsValid()
    {
        var result = validator.Validate(CreateCommand(-5, AdjustmentReason.Expiration, new string('N', InventoryMovement.NotesMaximumLength)));
        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(InventoryMovement.MaximumQuantity + 1)]
    [InlineData(-InventoryMovement.MaximumQuantity - 1)]
    public void Validate_WithDeltaZeroOrOutOfRange_FailsOnDeltaWithSpanishMessage(int delta)
    {
        var result = validator.Validate(CreateCommand(delta, AdjustmentReason.DataEntryError, null));
        result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            failure => failure.PropertyName.ShouldBe(nameof(RegisterAdjustmentRequest.Delta)),
            failure => failure.ErrorMessage.ShouldBe(ErrorMessages.AdjustmentDeltaInvalid));
    }

    [Theory]
    [InlineData((AdjustmentReason)0)]
    [InlineData((AdjustmentReason)7)]
    public void Validate_WithUndefinedReason_FailsOnReasonWithSpanishMessage(AdjustmentReason reason)
    {
        var result = validator.Validate(CreateCommand(5, reason, null));
        result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            failure => failure.PropertyName.ShouldBe(nameof(RegisterAdjustmentRequest.Reason)),
            failure => failure.ErrorMessage.ShouldBe(ErrorMessages.AdjustmentReasonInvalid));
    }

    [Fact]
    public void Validate_WithNotesOverMaximumLength_FailsOnNotesWithSpanishMessage()
    {
        var result = validator.Validate(CreateCommand(5, AdjustmentReason.Found, new string('N', InventoryMovement.NotesMaximumLength + 1)));
        result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            failure => failure.PropertyName.ShouldBe(nameof(RegisterAdjustmentRequest.Notes)),
            failure => failure.ErrorMessage.ShouldBe(ErrorMessages.MovementNotesTooLong));
    }

    private static RegisterAdjustmentCommand CreateCommand(int delta, AdjustmentReason reason, string? notes)
    {
        return new RegisterAdjustmentCommand(ProductId, new RegisterAdjustmentRequest(delta, reason, notes, null));
    }
}
