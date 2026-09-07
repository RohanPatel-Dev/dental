using Dental.Framework.Core.Domain;
using Dental.Modules.Billing.Contracts.Dtos;

namespace Dental.Modules.Billing.Domain;

/// <summary>A payment received against an invoice.</summary>
/// <remarks>
/// Reached only through <see cref="Invoice.Payments"/>, so its key is <c>ValueGeneratedNever()</c>.
/// </remarks>
public sealed class Payment : BaseEntity
{
    /// <summary>Invoice it was applied to.</summary>
    public Guid InvoiceId { get; set; }

    /// <summary>Amount received.</summary>
    public decimal Amount { get; set; }

    /// <summary>ISO 4217 currency.</summary>
    public string Currency { get; set; } = "USD";

    /// <summary>How it was taken.</summary>
    public PaymentMethod Method { get; set; }

    /// <summary>
    /// External reference, e.g. a card terminal receipt number. Never a card number: this system
    /// stores no cardholder data at all.
    /// </summary>
    public string? Reference { get; set; }

    /// <summary>When it was received.</summary>
    public DateTimeOffset ReceivedAt { get; set; }
}
