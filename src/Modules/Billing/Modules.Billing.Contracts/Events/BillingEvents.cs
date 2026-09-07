using Dental.Framework.Eventing.Abstractions;

namespace Dental.Modules.Billing.Contracts.Events;

/// <summary>Raised when an invoice is presented to a patient, so a notice can be sent.</summary>
/// <param name="InvoiceId">The invoice.</param>
/// <param name="PatientId">Patient billed.</param>
/// <param name="Number">Practice-visible invoice number.</param>
/// <param name="Total">Amount due.</param>
/// <param name="Currency">ISO 4217 currency.</param>
public sealed record InvoiceIssuedIntegrationEvent(
    Guid InvoiceId,
    Guid PatientId,
    string Number,
    decimal Total,
    string Currency) : IntegrationEvent;

/// <summary>Raised when an invoice is settled in full.</summary>
/// <param name="InvoiceId">The invoice.</param>
/// <param name="PatientId">Patient billed.</param>
/// <param name="Total">Amount settled.</param>
/// <param name="Currency">ISO 4217 currency.</param>
public sealed record InvoiceSettledIntegrationEvent(
    Guid InvoiceId,
    Guid PatientId,
    decimal Total,
    string Currency) : IntegrationEvent;
