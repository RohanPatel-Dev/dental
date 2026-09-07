using Dental.Framework.Core.Exceptions;
using Dental.Framework.Eventing.Outbox;
using Dental.Framework.Quota;
using Dental.Modules.Patients.Contracts.Dtos;
using Dental.Modules.Patients.Contracts.Events;
using Dental.Modules.Patients.Contracts.v1.Patients.RegisterPatient;
using Dental.Modules.Patients.Data;
using Dental.Modules.Patients.Domain;
using Dental.Modules.Patients.Services;
using Finbuckle.MultiTenant.Abstractions;
using Mediator;

namespace Dental.Modules.Patients.Features.v1.Patients.RegisterPatient;

/// <summary>Registers a patient and announces it so other modules can react.</summary>
/// <param name="context">The patients context.</param>
/// <param name="chartNumbers">Allocates the next chart number.</param>
/// <param name="outbox">Outbox writer.</param>
/// <param name="quotaService">Enforces the tenant's patient limit.</param>
/// <param name="tenantContextAccessor">Supplies the resolved tenant.</param>
/// <param name="timeProvider">Clock.</param>
public sealed class RegisterPatientCommandHandler(
    PatientsDbContext context,
    ChartNumberGenerator chartNumbers,
    IOutboxStore<PatientsDbContext> outbox,
    IQuotaService quotaService,
    IMultiTenantContextAccessor tenantContextAccessor,
    TimeProvider timeProvider) : ICommandHandler<RegisterPatientCommand, PatientDto>
{
    /// <inheritdoc />
    public async ValueTask<PatientDto> Handle(
        RegisterPatientCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        string tenantId = tenantContextAccessor.MultiTenantContext?.TenantInfo?.Id
            ?? throw new ForbiddenException("No tenant is resolved for this request.");

        QuotaUsage usage = await quotaService
            .GetUsageAsync(tenantId, QuotaResource.Patients, cancellationToken)
            .ConfigureAwait(false);

        if (usage.IsExceeded)
        {
            throw new CustomException(
                $"This practice has reached its limit of {usage.Limit} patient records.",
                System.Net.HttpStatusCode.TooManyRequests);
        }

        DateTimeOffset now = timeProvider.GetUtcNow();

        Patient patient = new()
        {
            ChartNumber = await chartNumbers.NextAsync(cancellationToken).ConfigureAwait(false),
            FirstName = command.FirstName.Trim(),
            LastName = command.LastName.Trim(),
            DateOfBirth = command.DateOfBirth,
            Sex = command.Sex,
            Email = command.Email?.Trim(),
            PhoneNumber = command.PhoneNumber?.Trim(),
            Status = PatientStatus.Active,
            PreferredProviderId = command.PreferredProviderId,
            Allergies = [.. command.Allergies],
            HasMarketingConsent = command.HasMarketingConsent,
            HasReminderConsent = command.HasReminderConsent,
            ConsentRecordedAt = now,
            TenantId = tenantId,
        };

        context.Patients.Add(patient);

        await outbox.AddAsync(
                new PatientRegisteredIntegrationEvent(
                    patient.Id,
                    patient.ChartNumber,
                    patient.FullName,
                    patient.Email,
                    patient.PhoneNumber,
                    patient.HasReminderConsent)
                {
                    TenantId = tenantId,
                    Source = nameof(Patients),
                },
                cancellationToken)
            .ConfigureAwait(false);

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return PatientMapper.ToDto(patient);
    }
}
