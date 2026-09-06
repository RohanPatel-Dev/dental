using FluentValidation;
using FluentValidation.Results;
using Mediator;

namespace Dental.Framework.Web.Validation;

/// <summary>
/// Runs every registered validator for a message BEFORE its handler, so a handler never has to
/// defend against shapes a validator already rejects.
/// </summary>
/// <typeparam name="TMessage">The command or query type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <param name="validators">Validators registered for the message.</param>
public sealed class ValidationBehavior<TMessage, TResponse>(
    IEnumerable<IValidator<TMessage>> validators) : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    /// <inheritdoc />
    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        IValidator<TMessage>[] applicable = [.. validators];
        if (applicable.Length == 0)
        {
            return await next(message, cancellationToken).ConfigureAwait(false);
        }

        ValidationContext<TMessage> context = new(message);

        ValidationResult[] results = await Task.WhenAll(
                applicable.Select(v => v.ValidateAsync(context, cancellationToken)))
            .ConfigureAwait(false);

        ValidationFailure[] failures = [.. results.SelectMany(r => r.Errors).Where(f => f is not null)];

        if (failures.Length > 0)
        {
            throw new ValidationException(failures);
        }

        return await next(message, cancellationToken).ConfigureAwait(false);
    }
}
