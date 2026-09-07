using Dental.Modules.Notifications.Data;
using Dental.Modules.Notifications.Domain;
using Microsoft.EntityFrameworkCore;

namespace Dental.Modules.Notifications.Services;

/// <summary>Reads and maintains this module's local patient projection.</summary>
/// <param name="context">The notifications context.</param>
public sealed class PatientContactProjection(NotificationsDbContext context)
{
    /// <summary>Reads one projected contact.</summary>
    /// <param name="patientId">Patient identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The contact, or null when the projection has not seen this patient.</returns>
    public Task<PatientContact?> FindAsync(Guid patientId, CancellationToken cancellationToken = default) =>
        context.PatientContacts.FirstOrDefaultAsync(c => c.PatientId == patientId, cancellationToken);

    /// <summary>Inserts or updates the projection row for a patient.</summary>
    /// <param name="patientId">Patient identifier.</param>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="apply">Mutates the row, whether new or existing.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The tracked row.</returns>
    /// <remarks>
    /// Events can arrive out of order or be redelivered, so this is an upsert rather than an insert.
    /// The inbox already guarantees each event is handled once per handler; the upsert covers the
    /// case where a contact-changed event overtakes the registration that created the patient.
    /// </remarks>
    public async Task<PatientContact> UpsertAsync(
        Guid patientId,
        string tenantId,
        Action<PatientContact> apply,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(apply);

        PatientContact? contact = await FindAsync(patientId, cancellationToken).ConfigureAwait(false);

        if (contact is null)
        {
            contact = new PatientContact
            {
                PatientId = patientId,
                FullName = string.Empty,
                TenantId = tenantId,
            };

            context.PatientContacts.Add(contact);
        }

        // An erased patient is terminal: no later event may resurrect their details.
        if (!contact.IsErased)
        {
            apply(contact);
        }

        return contact;
    }
}
