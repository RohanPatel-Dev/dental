using Dental.Modules.Clinical.Contracts.Dtos;

namespace Dental.Modules.Clinical.Contracts.Services;

/// <summary>The Clinical module's public surface.</summary>
public interface ITreatmentPlanService
{
    /// <summary>Reads one treatment plan.</summary>
    /// <param name="treatmentPlanId">Plan identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The plan, or null when it does not exist in this tenant.</returns>
    Task<TreatmentPlanDto?> GetAsync(
        Guid treatmentPlanId,
        CancellationToken cancellationToken = default);

    /// <summary>Reads the items delivered at one appointment, so they can be billed.</summary>
    /// <param name="appointmentId">Appointment identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The delivered items.</returns>
    Task<IReadOnlyList<DeliveredProcedureDto>> GetDeliveredAtAppointmentAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default);
}

/// <summary>One procedure actually delivered, in the shape Billing needs to raise a charge.</summary>
/// <param name="ProcedureId">Catalogued procedure.</param>
/// <param name="ProcedureCode">Procedure code.</param>
/// <param name="Description">What was done.</param>
/// <param name="ToothNumber">FDI tooth number, when it applies to one tooth.</param>
/// <param name="Fee">Fee to charge.</param>
/// <param name="Currency">ISO 4217 currency of the fee.</param>
public sealed record DeliveredProcedureDto(
    Guid ProcedureId,
    string ProcedureCode,
    string Description,
    int? ToothNumber,
    decimal Fee,
    string Currency);
