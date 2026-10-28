using Inventory.Application.Common.Messages;
using Inventory.Application.Products.Commands.DeactivateProduct;
using Inventory.Application.Products.Dtos;
using Inventory.Domain.Products;

namespace Inventory.Application.Tests.Products.Commands.DeactivateProduct;

public sealed class DeactivateProductValidatorTests
{
    private const int ProductId = 21;

    private readonly DeactivateProductValidator validator = new();

    [Theory]
    [InlineData(DeactivationReason.Expired, null)]
    [InlineData(DeactivationReason.Damaged, "")]
    [InlineData(DeactivationReason.Discontinued, "No longer sold")]
    [InlineData(DeactivationReason.Recalled, null)]
    [InlineData(DeactivationReason.Other, "Supplier recall")]
    public void Validate_WithValidRequest_IsValid(DeactivationReason reason, string? notes)
    {
        var result = validator.Validate(CreateCommand(reason, notes));
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithNotesAtMaximumLength_IsValid()
    {
        var result = validator.Validate(CreateCommand(DeactivationReason.Other, new string('N', Product.DeactivationNotesMaximumLength)));
        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Validate_WithUndefinedReason_FailsOnReasonWithSpanishMessage(byte reason)
    {
        var result = validator.Validate(CreateCommand((DeactivationReason)reason, null));
        result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            failure => failure.PropertyName.ShouldBe(nameof(DeactivateProductRequest.Reason)),
            failure => failure.ErrorMessage.ShouldBe(ErrorMessages.DeactivationReasonInvalid));
    }

    [Fact]
    public void Validate_WithNotesOverMaximumLength_FailsOnNotesWithSpanishMessage()
    {
        var result = validator.Validate(CreateCommand(DeactivationReason.Expired, new string('N', Product.DeactivationNotesMaximumLength + 1)));
        result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            failure => failure.PropertyName.ShouldBe(nameof(DeactivateProductRequest.Notes)),
            failure => failure.ErrorMessage.ShouldBe(ErrorMessages.DeactivationNotesTooLong));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithOtherReasonWithoutNotes_FailsOnNotesWithSpanishMessage(string? notes)
    {
        var result = validator.Validate(CreateCommand(DeactivationReason.Other, notes));
        result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            failure => failure.PropertyName.ShouldBe(nameof(DeactivateProductRequest.Notes)),
            failure => failure.ErrorMessage.ShouldBe(ErrorMessages.DeactivationNotesRequired));
    }

    private static DeactivateProductCommand CreateCommand(DeactivationReason reason, string? notes)
    {
        return new DeactivateProductCommand(ProductId, new DeactivateProductRequest(reason, notes));
    }
}
