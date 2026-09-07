using Dental.Modules.Billing.Contracts.Dtos;

namespace Dental.Modules.Billing.Contracts.Services;

/// <summary>The Billing module's public surface.</summary>
public interface IInvoiceService
{
    /// <summary>Reads one invoice.</summary>
    /// <param name="invoiceId">Invoice identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The invoice, or null when it does not exist in this tenant.</returns>
    Task<InvoiceDto?> GetAsync(Guid invoiceId, CancellationToken cancellationToken = default);

    /// <summary>Reads a patient's outstanding balance across every unsettled invoice.</summary>
    /// <param name="patientId">Patient identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The outstanding balance.</returns>
    Task<decimal> GetOutstandingBalanceAsync(
        Guid patientId,
        CancellationToken cancellationToken = default);
}
