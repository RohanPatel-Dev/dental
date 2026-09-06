using System.ComponentModel.DataAnnotations;

namespace Dental.Framework.Mailing;

/// <summary>SMTP configuration, bound from the <c>MailOptions</c> section.</summary>
public sealed class MailOptions
{
    /// <summary>SMTP host name.</summary>
    [Required]
    public string Host { get; set; } = "localhost";

    /// <summary>SMTP port.</summary>
    [Range(1, 65535)]
    public int Port { get; set; } = 587;

    /// <summary>SMTP user name, when the server requires authentication.</summary>
    public string? UserName { get; set; }

    /// <summary>SMTP password. Supply through user secrets or the environment, never source control.</summary>
    public string? Password { get; set; }

    /// <summary>Envelope from address.</summary>
    [Required]
    [EmailAddress]
    public string From { get; set; } = "no-reply@dental.local";

    /// <summary>Display name shown to recipients.</summary>
    public string DisplayName { get; set; } = "Dental";

    /// <summary>Whether to negotiate STARTTLS.</summary>
    public bool UseStartTls { get; set; } = true;

    /// <summary>
    /// When true, messages are logged instead of sent. The default for local development, so a test
    /// tenant can never mail a real patient.
    /// </summary>
    public bool DeliverToLog { get; set; }
}
