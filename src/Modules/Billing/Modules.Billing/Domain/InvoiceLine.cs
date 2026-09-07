using Dental.Framework.Core.Domain;

namespace Dental.Modules.Billing.Domain;

/// <summary>One charge on an invoice.</summary>
/// <remarks>
/// Reached only through <see cref="Invoice.Lines"/>, so its key is <c>ValueGeneratedNever()</c>.
/// </remarks>
public sealed class InvoiceLine : BaseEntity
{
    /// <summary>Owning invoice.</summary>
    public Guid InvoiceId { get; set; }

    /// <summary>Procedure code the charge came from.</summary>
    public string ProcedureCode { get; set; } = default!;

    /// <summary>What is being charged for.</summary>
    public string Description { get; set; } = default!;

    /// <summary>FDI tooth number, when the charge applies to one tooth.</summary>
    public int? ToothNumber { get; set; }

    /// <summary>Amount charged.</summary>
    public decimal Amount { get; set; }

    /// <summary>Appointment the work was delivered at, for reconciliation.</summary>
    public Guid? AppointmentId { get; set; }
}
