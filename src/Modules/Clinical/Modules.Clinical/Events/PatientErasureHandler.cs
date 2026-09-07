using Dental.Framework.Eventing.Abstractions;
using Dental.Modules.Clinical.Data;
using Dental.Modules.Clinical.Domain;
using Dental.Modules.Patients.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dental.Modules.Clinical.Events;

/// <summary>
/// Strips free-text clinical notes when a patient's record is erased, while keeping the structured
/// treatment history.
/// </summary>
/// <remarks>
/// A dental practice has a statutory duty to retain what was done to a patient's teeth. The lawful
/// response to an erasure request is therefore to remove the free text - which is where names,
/// addresses and third parties tend to end up - and keep the coded clinical facts.
/// </remarks>
/// <param name="context">The clinical context.</param>
/// <param name="logger">Logger.</param>
public sealed class PatientErasureHandler(
    ClinicalDbContext context,
    ILogger<PatientErasureHandler> logger)
    : IIntegrationEventHandler<PatientErasureRequestedIntegrationEvent>
{
    /// <inheritdoc />
    public async Task HandleAsync(
        PatientErasureRequestedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        List<TreatmentPlan> plans = await context.TreatmentPlans
            .Where(p => p.PatientId == integrationEvent.PatientId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (TreatmentPlan plan in plans)
        {
            plan.Erase();
        }

        List<ChartEntry> entries = await context.ChartEntries
            .Where(e => e.PatientId == integrationEvent.PatientId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (ChartEntry entry in entries)
        {
            entry.Erase();
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Erased clinical free text for patient {PatientId}: {PlanCount} plan(s), {EntryCount} chart entries.",
            integrationEvent.PatientId,
            plans.Count,
            entries.Count);
    }
}
