using Dental.Framework.Core.Exceptions;
using Dental.Modules.Patients.Contracts.Dtos;
using Dental.Modules.Patients.Contracts.v1.Patients.GetPatient;
using Dental.Modules.Patients.Data;
using Dental.Modules.Patients.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Dental.Modules.Patients.Features.v1.Patients.GetPatient;

/// <summary>Reads one patient record.</summary>
/// <param name="context">The patients context.</param>
public sealed class GetPatientQueryHandler(PatientsDbContext context)
    : IQueryHandler<GetPatientQuery, PatientDto>
{
    /// <inheritdoc />
    public async ValueTask<PatientDto> Handle(GetPatientQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        // The tenant query filter turns "belongs to another practice" into "does not exist", which
        // is exactly the response a cross-tenant probe should get.
        Patient patient = await context.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == query.PatientId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw NotFoundException.For("Patient", query.PatientId);

        return PatientMapper.ToDto(patient);
    }
}
