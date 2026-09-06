namespace Dental.Framework.Mailing;

/// <summary>Sends transactional mail. Enqueue on the <c>email</c> job queue rather than blocking a request.</summary>
public interface IMailService
{
    /// <summary>Sends one message.</summary>
    /// <param name="request">The message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the server has accepted the message.</returns>
    Task SendAsync(MailRequest request, CancellationToken cancellationToken = default);

    /// <summary>Renders a template and sends the result.</summary>
    /// <typeparam name="TModel">Template model type.</typeparam>
    /// <param name="mailTemplate">The template to render.</param>
    /// <param name="model">Model supplying the placeholders.</param>
    /// <param name="recipients">Recipient addresses.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the server has accepted the message.</returns>
    Task SendTemplatedAsync<TModel>(
        IMailTemplate<TModel> mailTemplate,
        TModel model,
        IReadOnlyCollection<string> recipients,
        CancellationToken cancellationToken = default);
}

/// <summary>A renderable mail template.</summary>
/// <typeparam name="TModel">Model supplying the placeholders.</typeparam>
public interface IMailTemplate<in TModel>
{
    /// <summary>Renders the subject line.</summary>
    /// <param name="model">The model.</param>
    /// <returns>The subject.</returns>
    string RenderSubject(TModel model);

    /// <summary>Renders the HTML body.</summary>
    /// <param name="model">The model.</param>
    /// <returns>The HTML body.</returns>
    string RenderHtml(TModel model);
}
