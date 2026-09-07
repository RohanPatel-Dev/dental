using Dental.Framework.Core.Domain;
using Dental.Framework.Core.Exceptions;
using Dental.Framework.Core.ValueObjects;
using Dental.Modules.Billing.Contracts.Dtos;

namespace Dental.Modules.Billing.Domain;

/// <summary>A patient's invoice: the charges raised and the payments received against them.</summary>
public sealed class Invoice : AggregateRoot, IAuditableEntity, ISoftDeletable
{
    /// <summary>Practice-visible invoice number, unique within the tenant.</summary>
    public string Number { get; set; } = default!;

    /// <summary>Patient billed.</summary>
    public Guid PatientId { get; set; }

    /// <summary>Where it is in its lifecycle.</summary>
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    /// <summary>ISO 4217 currency every line and payment is in.</summary>
    public string Currency { get; set; } = "USD";

    /// <summary>When it was presented to the patient.</summary>
    public DateTimeOffset? IssuedAt { get; set; }

    /// <summary>Why it was voided, when it was.</summary>
    public string? VoidReason { get; set; }

    /// <summary>The charges.</summary>
    public List<InvoiceLine> Lines { get; } = [];

    /// <summary>The payments received.</summary>
    public List<Payment> Payments { get; } = [];

    /// <inheritdoc />
    public Guid? CreatedBy { get; set; }

    /// <inheritdoc />
    public Guid? UpdatedBy { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? DeletedAt { get; set; }

    /// <inheritdoc />
    public Guid? DeletedBy { get; set; }

    /// <summary>Sum of the charges.</summary>
    public decimal Total => Lines.Sum(l => l.Amount);

    /// <summary>How much has been received.</summary>
    public decimal AmountPaid => Payments.Sum(p => p.Amount);

    /// <summary>What remains outstanding.</summary>
    public decimal Balance => Total - AmountPaid;

    /// <summary>Adds a charge. Only a draft invoice accepts new lines.</summary>
    /// <param name="line">The charge to add.</param>
    /// <exception cref="ConflictException">The invoice is no longer a draft.</exception>
    public void AddLine(InvoiceLine line)
    {
        ArgumentNullException.ThrowIfNull(line);

        if (Status != InvoiceStatus.Draft)
        {
            throw new ConflictException(
                $"Charges cannot be added to an invoice that is {Status}.");
        }

        Lines.Add(line);
    }

    /// <summary>Presents the invoice to the patient.</summary>
    /// <param name="issuedAt">When it is being presented.</param>
    /// <exception cref="ConflictException">The invoice is not an issuable draft.</exception>
    public void Issue(DateTimeOffset issuedAt)
    {
        if (Status != InvoiceStatus.Draft)
        {
            throw new ConflictException($"An invoice that is {Status} cannot be issued.");
        }

        if (Lines.Count == 0)
        {
            throw new ConflictException("An invoice with no charges cannot be issued.");
        }

        Status = InvoiceStatus.Issued;
        IssuedAt = issuedAt;
    }

    /// <summary>Applies a payment and moves the invoice's status accordingly.</summary>
    /// <param name="payment">The payment received.</param>
    /// <exception cref="ConflictException">The invoice cannot take a payment, or the payment overpays it.</exception>
    public void ApplyPayment(Payment payment)
    {
        ArgumentNullException.ThrowIfNull(payment);

        if (Status is InvoiceStatus.Voided or InvoiceStatus.Paid)
        {
            throw new ConflictException($"An invoice that is {Status} cannot take a payment.");
        }

        // Amounts are Money so a currency mismatch throws here rather than producing a wrong total.
        Money outstanding = new(Balance, Currency);
        Money received = new(payment.Amount, payment.Currency);

        if (received.Amount > outstanding.Amount)
        {
            throw new ConflictException(
                $"The payment of {received} exceeds the outstanding balance of {outstanding}.");
        }

        Payments.Add(payment);

        Status = Balance <= 0 ? InvoiceStatus.Paid : InvoiceStatus.PartiallyPaid;

        if (Status == InvoiceStatus.Paid)
        {
            RaiseDomainEvent(new InvoiceSettledDomainEvent(Id, PatientId, Total, Currency));
        }
    }

    /// <summary>Cancels the invoice without payment.</summary>
    /// <param name="reason">Why it is being voided.</param>
    /// <exception cref="ConflictException">Money has already been received against it.</exception>
    public void Void(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (AmountPaid > 0)
        {
            throw new ConflictException(
                "An invoice with payments against it cannot be voided; refund the payments first.");
        }

        Status = InvoiceStatus.Voided;
        VoidReason = reason;
    }
}

/// <summary>Raised in-process when an invoice is settled in full.</summary>
/// <param name="InvoiceId">The invoice.</param>
/// <param name="PatientId">Patient billed.</param>
/// <param name="Total">Amount settled.</param>
/// <param name="Currency">ISO 4217 currency.</param>
public sealed record InvoiceSettledDomainEvent(
    Guid InvoiceId,
    Guid PatientId,
    decimal Total,
    string Currency) : DomainEvent;
