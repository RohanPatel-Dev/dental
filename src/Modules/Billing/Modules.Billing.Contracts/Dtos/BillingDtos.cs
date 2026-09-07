namespace Dental.Modules.Billing.Contracts.Dtos;

/// <summary>Where an invoice is in its lifecycle.</summary>
public enum InvoiceStatus
{
    /// <summary>Accumulating charges, not yet presented.</summary>
    Draft = 0,

    /// <summary>Presented to the patient and awaiting payment.</summary>
    Issued = 1,

    /// <summary>Partly paid.</summary>
    PartiallyPaid = 2,

    /// <summary>Settled in full.</summary>
    Paid = 3,

    /// <summary>Cancelled without payment.</summary>
    Voided = 4,
}

/// <summary>How a payment was taken.</summary>
public enum PaymentMethod
{
    /// <summary>Cash.</summary>
    Cash = 0,

    /// <summary>Card, present or not.</summary>
    Card = 1,

    /// <summary>Bank transfer.</summary>
    BankTransfer = 2,

    /// <summary>Paid by an insurer.</summary>
    Insurance = 3,

    /// <summary>Written off.</summary>
    WriteOff = 4,
}

/// <summary>One charge on an invoice.</summary>
/// <param name="Id">Line identifier.</param>
/// <param name="ProcedureCode">Procedure code the charge came from.</param>
/// <param name="Description">What is being charged for.</param>
/// <param name="ToothNumber">FDI tooth number, when the charge applies to one tooth.</param>
/// <param name="Amount">Amount charged.</param>
public sealed record InvoiceLineDto(
    Guid Id,
    string ProcedureCode,
    string Description,
    int? ToothNumber,
    decimal Amount);

/// <summary>A patient's invoice.</summary>
/// <param name="Id">Invoice identifier.</param>
/// <param name="Number">Practice-visible invoice number.</param>
/// <param name="PatientId">Patient billed.</param>
/// <param name="Status">Where it is in its lifecycle.</param>
/// <param name="Lines">The charges.</param>
/// <param name="Total">Sum of the charges.</param>
/// <param name="AmountPaid">How much has been received.</param>
/// <param name="Balance">What remains outstanding.</param>
/// <param name="Currency">ISO 4217 currency.</param>
/// <param name="IssuedAt">When it was presented to the patient.</param>
/// <param name="CreatedAt">When it was raised.</param>
public sealed record InvoiceDto(
    Guid Id,
    string Number,
    Guid PatientId,
    InvoiceStatus Status,
    IReadOnlyList<InvoiceLineDto> Lines,
    decimal Total,
    decimal AmountPaid,
    decimal Balance,
    string Currency,
    DateTimeOffset? IssuedAt,
    DateTimeOffset CreatedAt);

/// <summary>A payment received against an invoice.</summary>
/// <param name="Id">Payment identifier.</param>
/// <param name="InvoiceId">Invoice it was applied to.</param>
/// <param name="Amount">Amount received.</param>
/// <param name="Currency">ISO 4217 currency.</param>
/// <param name="Method">How it was taken.</param>
/// <param name="Reference">External reference, e.g. a card terminal receipt number.</param>
/// <param name="ReceivedAt">When it was received.</param>
public sealed record PaymentDto(
    Guid Id,
    Guid InvoiceId,
    decimal Amount,
    string Currency,
    PaymentMethod Method,
    string? Reference,
    DateTimeOffset ReceivedAt);
