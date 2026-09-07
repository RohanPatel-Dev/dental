using Dental.Framework.Core.Exceptions;
using Dental.Framework.Eventing.Outbox;
using Dental.Framework.Web.Realtime;
using Dental.Modules.Patients.Contracts.Services;
using Dental.Modules.Scheduling.Contracts.Dtos;
using Dental.Modules.Scheduling.Contracts.Events;
using Dental.Modules.Scheduling.Contracts.v1.Appointments.BookAppointment;
using Dental.Modules.Scheduling.Data;
using Dental.Modules.Scheduling.Domain;
using Dental.Modules.Scheduling.Services;
using Finbuckle.MultiTenant.Abstractions;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Dental.Modules.Scheduling.Features.v1.Appointments.BookAppointment;

/// <summary>Books an appointment after checking the patient, the chair and the slot.</summary>
/// <param name="context">The scheduling context.</param>
/// <param name="patientService">Confirms the patient exists in this tenant.</param>
/// <param name="availability">Rejects a double booking.</param>
/// <param name="appointments">Projects the result.</param>
/// <param name="outbox">Outbox writer.</param>
/// <param name="realtime">Pushes the new booking to open day views.</param>
/// <param name="tenantContextAccessor">Supplies the resolved tenant.</param>
public sealed class BookAppointmentCommandHandler(
    SchedulingDbContext context,
    IPatientService patientService,
    SlotAvailabilityChecker availability,
    AppointmentService appointments,
    IOutboxStore outbox,
    IRealtimeNotifier realtime,
    IMultiTenantContextAccessor tenantContextAccessor)
    : ICommandHandler<BookAppointmentCommand, AppointmentDto>
{
    /// <inheritdoc />
    public async ValueTask<AppointmentDto> Handle(
        BookAppointmentCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        string tenantId = tenantContextAccessor.MultiTenantContext?.TenantInfo?.Id
            ?? throw new ForbiddenException("No tenant is resolved for this request.");

        // Cross-module check through the contract, never a join into the patients schema.
        if (!await patientService.ExistsAsync(command.PatientId, cancellationToken).ConfigureAwait(false))
        {
            throw NotFoundException.For("Patient", command.PatientId);
        }

        bool providerExists = await context.Providers
            .AnyAsync(p => p.Id == command.ProviderId && p.IsAcceptingPatients, cancellationToken)
            .ConfigureAwait(false);

        if (!providerExists)
        {
            throw NotFoundException.For("Provider", command.ProviderId);
        }

        bool operatoryExists = await context.Operatories
            .AnyAsync(o => o.Id == command.OperatoryId && o.IsActive, cancellationToken)
            .ConfigureAwait(false);

        if (!operatoryExists)
        {
            throw NotFoundException.For("Operatory", command.OperatoryId);
        }

        DateTimeOffset endsAt = command.StartsAt.AddMinutes(command.DurationMinutes);

        await availability
            .EnsureFreeAsync(
                command.ProviderId,
                command.OperatoryId,
                command.StartsAt,
                endsAt,
                excludingAppointmentId: null,
                cancellationToken)
            .ConfigureAwait(false);

        Appointment appointment = new()
        {
            PatientId = command.PatientId,
            ProviderId = command.ProviderId,
            OperatoryId = command.OperatoryId,
            StartsAt = command.StartsAt,
            EndsAt = endsAt,
            Kind = command.Kind,
            Status = AppointmentStatus.Scheduled,
            Notes = command.Notes,
            TenantId = tenantId,
        };

        context.Appointments.Add(appointment);

        await outbox.AddAsync(
                new AppointmentBookedIntegrationEvent(
                    appointment.Id,
                    appointment.PatientId,
                    appointment.ProviderId,
                    appointment.StartsAt,
                    appointment.Kind)
                {
                    TenantId = tenantId,
                    Source = nameof(Scheduling),
                },
                cancellationToken)
            .ConfigureAwait(false);

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        IReadOnlyList<AppointmentDto> hydrated = await appointments
            .HydrateAsync([appointment], cancellationToken)
            .ConfigureAwait(false);

        // Broadcast to the tenant group, never Clients.All.
        await realtime
            .SendToTenantAsync(tenantId, RealtimeEvents.AppointmentBooked, hydrated[0], cancellationToken)
            .ConfigureAwait(false);

        return hydrated[0];
    }
}
