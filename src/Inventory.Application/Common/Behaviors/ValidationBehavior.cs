using FluentValidation;
using FluentValidation.Results;
using Mediator;

namespace Inventory.Application.Common.Behaviors;

public sealed class ValidationBehavior<TMessage, TResponse>(IEnumerable<IValidator<TMessage>> validators) : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    public async ValueTask<TResponse> Handle(TMessage message, MessageHandlerDelegate<TMessage, TResponse> next, CancellationToken cancellationToken)
    {
        var failures = await CollectFailuresAsync(message, cancellationToken);
        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }

        return await next(message, cancellationToken);
    }

    private async Task<List<ValidationFailure>> CollectFailuresAsync(TMessage message, CancellationToken cancellationToken)
    {
        var failures = new List<ValidationFailure>();
        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(message, cancellationToken);
            failures.AddRange(result.Errors);
        }

        return failures;
    }
}
