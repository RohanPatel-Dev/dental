using Dental.Modules.Patients.Contracts.v1.Patients.GetPatient;
using FluentValidation;

namespace Dental.Modules.Patients.Features.v1.Patients.GetPatient;

/// <summary>Validates <see cref="GetPatientQuery"/>.</summary>
public sealed class GetPatientQueryValidator : AbstractValidator<GetPatientQuery>
{
    /// <summary>Builds the rules.</summary>
    public GetPatientQueryValidator() => RuleFor(q => q.PatientId).NotEmpty();
}
