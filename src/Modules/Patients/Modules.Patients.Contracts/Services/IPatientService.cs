using Dental.Modules.Patients.Contracts.Dtos;

namespace Dental.Modules.Patients.Contracts.Services;

/// <summary>
/// The Patients module's public surface.
/// </summary>
/// <remarks>
/// Deliberately returns <see cref="PatientSummaryDto"/> rather than the full record: Scheduling and
/// Billing need a name and a contact method, not a medical history. Keeping the clinical detail out
/// of the shared contract is the cheapest privacy control available.
/// </remarks>
public interface IPatientService
{
    /// <summary>Reads one patient summary.</summary>
    /// <param name="patientId">Patient identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The summary, or null when the patient does not exist in this tenant.</returns>
    Task<PatientSummaryDto?> GetSummaryAsync(
        Guid patientId,
        CancellationToken cancellationToken = default);

    /// <summary>Reads several patient summaries at once, for list projections.</summary>
    /// <param name="patientIds">Patient identifiers.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The summaries that exist, keyed by identifier.</returns>
    Task<IReadOnlyDictionary<Guid, PatientSummaryDto>> GetSummariesAsync(
        IReadOnlyCollection<Guid> patientIds,
        CancellationToken cancellationToken = default);

    /// <summary>Checks that a patient exists and is active in the current tenant.</summary>
    /// <param name="patientId">Patient identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> when the patient is active here.</returns>
    Task<bool> ExistsAsync(Guid patientId, CancellationToken cancellationToken = default);

    /// <summary>Counts patient records in a tenant. Used by the quota gauge.</summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of records.</returns>
    Task<long> CountAsync(string tenantId, CancellationToken cancellationToken = default);
}
