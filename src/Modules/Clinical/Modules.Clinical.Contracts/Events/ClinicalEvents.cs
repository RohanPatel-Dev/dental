using Dental.Framework.Eventing.Abstractions;

namespace Dental.Modules.Clinical.Contracts.Events;

/// <summary>
/// Raised when procedures are delivered at an appointment, so Billing can raise the charges.
/// </summary>
/// <param name="AppointmentId">Appointment the work was delivered at.</param>
/// <param name="PatientId">Patient treated.</param>
/// <param name="ProviderId">Provider who delivered the work.</param>
/// <param name="Procedures">What was delivered, with the fee for each.</param>
public sealed record ProceduresDeliveredIntegrationEvent(
    Guid AppointmentId,
    Guid PatientId,
    Guid ProviderId,
    IReadOnlyList<DeliveredProcedurePayload> Procedures) : IntegrationEvent;

/// <summary>One delivered procedure inside <see cref="ProceduresDeliveredIntegrationEvent"/>.</summary>
/// <param name="ProcedureId">Catalogued procedure.</param>
/// <param name="ProcedureCode">Procedure code.</param>
/// <param name="Description">What was done.</param>
/// <param name="ToothNumber">FDI tooth number, when it applies to one tooth.</param>
/// <param name="Fee">Fee to charge.</param>
/// <param name="Currency">ISO 4217 currency of the fee.</param>
public sealed record DeliveredProcedurePayload(
    Guid ProcedureId,
    string ProcedureCode,
    string Description,
    int? ToothNumber,
    decimal Fee,
    string Currency);

/// <summary>Raised when a patient accepts a treatment plan.</summary>
/// <param name="TreatmentPlanId">The accepted plan.</param>
/// <param name="PatientId">Patient who accepted it.</param>
/// <param name="TotalFee">Total quoted.</param>
/// <param name="Currency">ISO 4217 currency of the quote.</param>
public sealed record TreatmentPlanAcceptedIntegrationEvent(
    Guid TreatmentPlanId,
    Guid PatientId,
    decimal TotalFee,
    string Currency) : IntegrationEvent;
