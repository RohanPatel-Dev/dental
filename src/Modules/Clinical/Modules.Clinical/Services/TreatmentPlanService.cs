using Dental.Modules.Clinical.Contracts.Dtos;
using Dental.Modules.Clinical.Contracts.Services;
using Dental.Modules.Clinical.Data;
using Dental.Modules.Clinical.Domain;
using Microsoft.EntityFrameworkCore;

namespace Dental.Modules.Clinical.Services;

/// <summary>The Clinical module's implementation of its own public contract.</summary>
/// <param name="context">The clinical context.</param>
public sealed class TreatmentPlanService(ClinicalDbContext context) : ITreatmentPlanService
{
    /// <inheritdoc />
    public async Task<TreatmentPlanDto?> GetAsync(
        Guid treatmentPlanId,
        CancellationToken cancellationToken = default)
    {
        TreatmentPlan? plan = await context.TreatmentPlans
            .AsNoTracking()
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == treatmentPlanId, cancellationToken)
            .ConfigureAwait(false);

        return plan is null ? null : Map(plan);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DeliveredProcedureDto>> GetDeliveredAtAppointmentAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        List<DeliveredProcedureDto> delivered = await context.TreatmentPlanItems
            .AsNoTracking()
            .Where(i => i.DeliveredAtAppointmentId == appointmentId && i.IsDelivered)
            .Join(
                context.TreatmentPlans.AsNoTracking(),
                item => item.TreatmentPlanId,
                plan => plan.Id,
                (item, plan) => new DeliveredProcedureDto(
                    item.ProcedureId,
                    item.ProcedureCode,
                    item.Description,
                    item.ToothNumber,
                    item.Fee,
                    plan.Currency))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return delivered;
    }

    internal static TreatmentPlanDto Map(TreatmentPlan plan) =>
        new(
            plan.Id,
            plan.PatientId,
            plan.ProviderId,
            plan.Status,
            [
                .. plan.Items.Select(i => new TreatmentPlanItemDto(
                    i.Id,
                    i.ProcedureId,
                    i.ProcedureCode,
                    i.ToothNumber,
                    i.Surfaces,
                    i.Fee,
                    i.IsDelivered)),
            ],
            plan.TotalFee,
            plan.Currency,
            plan.Notes,
            plan.CreatedAt,
            plan.AcceptedAt);
}
