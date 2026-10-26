using FluentValidation;
using Inventory.Application.Common.Behaviors;
using Mediator;
using NSubstitute;

namespace Inventory.Application.Tests.Common.Behaviors;

public sealed class ValidationBehaviorTests
{
    private const string HandledResponse = "handled";

    private readonly MessageHandlerDelegate<SampleMessage, string> next = (_, _) => ValueTask.FromResult(HandledResponse);

    private static SampleMessage InvalidMessage => new(string.Empty, string.Empty);

    private static SampleMessage ValidMessage => new("Milk", "FOOD-001");

    [Fact]
    public async Task Handle_WhenThereAreNoValidators_CallsNext()
    {
        var behavior = new ValidationBehavior<SampleMessage, string>([]);
        var response = await behavior.Handle(ValidMessage, next, CancellationToken.None);
        response.ShouldBe(HandledResponse);
    }

    [Fact]
    public async Task Handle_WhenAllValidatorsPass_CallsNext()
    {
        var behavior = new ValidationBehavior<SampleMessage, string>([CreateNameValidator(), CreateSkuValidator()]);
        var response = await behavior.Handle(ValidMessage, next, CancellationToken.None);
        response.ShouldBe(HandledResponse);
    }

    [Fact]
    public async Task Handle_WhenValidatorsFail_ThrowsValidationExceptionWithEveryFailure()
    {
        var behavior = new ValidationBehavior<SampleMessage, string>([CreateNameValidator(), CreateSkuValidator()]);
        var exception = await Should.ThrowAsync<ValidationException>(behavior.Handle(InvalidMessage, next, CancellationToken.None).AsTask());
        exception.Errors.Select(failure => failure.PropertyName).ShouldBe(["Name", "Sku"], ignoreOrder: true);
    }

    [Fact]
    public async Task Handle_WhenValidationFails_DoesNotCallNext()
    {
        var spyNext = Substitute.For<MessageHandlerDelegate<SampleMessage, string>>();
        var behavior = new ValidationBehavior<SampleMessage, string>([CreateNameValidator()]);
        await Should.ThrowAsync<ValidationException>(behavior.Handle(InvalidMessage, spyNext, CancellationToken.None).AsTask());
        await spyNext.DidNotReceive().Invoke(Arg.Any<SampleMessage>(), Arg.Any<CancellationToken>());
    }

    private static InlineValidator<SampleMessage> CreateNameValidator()
    {
        return new InlineValidator<SampleMessage> { validator => validator.RuleFor(message => message.Name).NotEmpty() };
    }

    private static InlineValidator<SampleMessage> CreateSkuValidator()
    {
        return new InlineValidator<SampleMessage> { validator => validator.RuleFor(message => message.Sku).NotEmpty() };
    }
}
