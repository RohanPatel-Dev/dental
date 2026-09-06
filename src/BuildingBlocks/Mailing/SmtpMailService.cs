using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Dental.Framework.Mailing;

/// <summary>MailKit backed <see cref="IMailService"/>.</summary>
/// <param name="options">SMTP configuration.</param>
/// <param name="logger">Logger.</param>
public sealed class SmtpMailService(IOptions<MailOptions> options, ILogger<SmtpMailService> logger)
    : IMailService
{
    private readonly MailOptions _options = options.Value;

    /// <inheritdoc />
    public async Task SendAsync(MailRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.To.Count == 0)
        {
            throw new ArgumentException("A mail request needs at least one recipient.", nameof(request));
        }

        if (_options.DeliverToLog)
        {
            logger.LogInformation(
                "Mail suppressed (DeliverToLog): subject {Subject} to {RecipientCount} recipient(s).",
                request.Subject,
                request.To.Count);
            return;
        }

        using MimeMessage message = BuildMessage(request);
        using SmtpClient client = new();

        await client.ConnectAsync(
                _options.Host,
                _options.Port,
                _options.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto,
                cancellationToken)
            .ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(_options.UserName))
        {
            await client.AuthenticateAsync(_options.UserName, _options.Password ?? string.Empty, cancellationToken)
                .ConfigureAwait(false);
        }

        await client.SendAsync(message, cancellationToken).ConfigureAwait(false);
        await client.DisconnectAsync(quit: true, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Sent mail with subject {Subject} to {RecipientCount} recipient(s).",
            request.Subject,
            request.To.Count);
    }

    /// <inheritdoc />
    public Task SendTemplatedAsync<TModel>(
        IMailTemplate<TModel> mailTemplate,
        TModel model,
        IReadOnlyCollection<string> recipients,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mailTemplate);
        ArgumentNullException.ThrowIfNull(recipients);

        MailRequest request = new()
        {
            Subject = mailTemplate.RenderSubject(model),
            HtmlBody = mailTemplate.RenderHtml(model),
        };

        foreach (string recipient in recipients)
        {
            request.To.Add(recipient);
        }

        return SendAsync(request, cancellationToken);
    }

    private MimeMessage BuildMessage(MailRequest request)
    {
        MimeMessage message = new();
        message.From.Add(new MailboxAddress(_options.DisplayName, _options.From));

        foreach (string to in request.To)
        {
            message.To.Add(MailboxAddress.Parse(to));
        }

        foreach (string cc in request.Cc)
        {
            message.Cc.Add(MailboxAddress.Parse(cc));
        }

        foreach (string bcc in request.Bcc)
        {
            message.Bcc.Add(MailboxAddress.Parse(bcc));
        }

        if (!string.IsNullOrWhiteSpace(request.ReplyTo))
        {
            message.ReplyTo.Add(MailboxAddress.Parse(request.ReplyTo));
        }

        message.Subject = request.Subject;

        BodyBuilder body = new() { HtmlBody = request.HtmlBody };
        if (!string.IsNullOrWhiteSpace(request.TextBody))
        {
            body.TextBody = request.TextBody;
        }

        message.Body = body.ToMessageBody();

        return message;
    }
}
