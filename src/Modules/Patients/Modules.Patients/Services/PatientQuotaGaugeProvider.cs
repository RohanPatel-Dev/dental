using Dental.Framework.Quota;
using Dental.Modules.Patients.Contracts.Services;

namespace Dental.Modules.Patients.Services;

/// <summary>Reports the tenant's patient count to the quota subsystem.</summary>
/// <param name="patientService">Counts patient records.</param>
public sealed class PatientQuotaGaugeProvider(IPatientService patientService) : IQuotaGaugeProvider
{
    /// <inheritdoc />
    public QuotaResource Resource => QuotaResource.Patients;

    /// <inheritdoc />
    public Task<long> GetCurrentAsync(string tenantId, CancellationToken cancellationToken = default) =>
        patientService.CountAsync(tenantId, cancellationToken);
}
