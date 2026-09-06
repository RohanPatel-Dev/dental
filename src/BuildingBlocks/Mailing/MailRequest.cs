namespace Dental.Framework.Mailing;

/// <summary>One outbound message.</summary>
public sealed class MailRequest
{
    /// <summary>Primary recipients.</summary>
    public IList<string> To { get; } = [];

    /// <summary>Carbon copy recipients.</summary>
    public IList<string> Cc { get; } = [];

    /// <summary>Blind carbon copy recipients.</summary>
    public IList<string> Bcc { get; } = [];

    /// <summary>Subject line.</summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>HTML body.</summary>
    public string HtmlBody { get; set; } = string.Empty;

    /// <summary>Plain text alternative, generated from the HTML when omitted.</summary>
    public string? TextBody { get; set; }

    /// <summary>Reply-to address, when it differs from the configured sender.</summary>
    public string? ReplyTo { get; set; }
}
