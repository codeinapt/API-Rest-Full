using Inventory.Application.Common.Messages;
using Inventory.Application.Movements.Commands.RegisterMovement;
using Inventory.Application.Movements.Dtos;
using Inventory.Domain.Movements;

namespace Inventory.Application.Tests.Movements.Commands.RegisterMovement;

public sealed class RegisterMovementValidatorTests
{
    private const int ProductId = 21;

    private readonly RegisterMovementValidator validator = new();

    [Theory]
    [InlineData(MovementType.In, 1, null)]
    [InlineData(MovementType.Out, 1, "")]
    [InlineData(MovementType.In, InventoryMovement.MaximumQuantity, "Supplier delivery")]
    [InlineData(MovementType.Out, 250, "Shipment to store")]
    public void Validate_WithValidRequest_IsValid(MovementType type, int quantity, string? notes)
    {
        var result = validator.Validate(CreateCommand(type, quantity, notes));
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithNotesAtMaximumLength_IsValid()
    {
        var result = validator.Validate(CreateCommand(MovementType.In, 5, new string('N', InventoryMovement.NotesMaximumLength)));
        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(MovementType.Adjustment)]
    [InlineData((MovementType)0)]
    [InlineData((MovementType)4)]
    public void Validate_WithTypeOtherThanInOrOut_FailsOnTypeWithSpanishMessage(MovementType type)
    {
        var result = validator.Validate(CreateCommand(type, 5, null));
        result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            failure => failure.PropertyName.ShouldBe(nameof(RegisterMovementRequest.Type)),
            failure => failure.ErrorMessage.ShouldBe(ErrorMessages.MovementTypeInvalid));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(InventoryMovement.MaximumQuantity + 1)]
    public void Validate_WithQuantityOutOfRange_FailsOnQuantityWithSpanishMessage(int quantity)
    {
        var result = validator.Validate(CreateCommand(MovementType.Out, quantity, null));
        result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            failure => failure.PropertyName.ShouldBe(nameof(RegisterMovementRequest.Quantity)),
            failure => failure.ErrorMessage.ShouldBe(ErrorMessages.MovementQuantityInvalid));
    }

    [Fact]
    public void Validate_WithNotesOverMaximumLength_FailsOnNotesWithSpanishMessage()
    {
        var result = validator.Validate(CreateCommand(MovementType.In, 5, new string('N', InventoryMovement.NotesMaximumLength + 1)));
        result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            failure => failure.PropertyName.ShouldBe(nameof(RegisterMovementRequest.Notes)),
            failure => failure.ErrorMessage.ShouldBe(ErrorMessages.MovementNotesTooLong));
    }

    private static RegisterMovementCommand CreateCommand(MovementType type, int quantity, string? notes)
    {
        return new RegisterMovementCommand(ProductId, new RegisterMovementRequest(type, quantity, notes));
    }
}
