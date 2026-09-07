using Dental.Framework.Core.Exceptions;
using Dental.Modules.Clinical.Contracts.Dtos;
using Dental.Modules.Clinical.Contracts.v1.TreatmentPlans.CreateTreatmentPlan;
using Dental.Modules.Clinical.Data;
using Dental.Modules.Clinical.Domain;
using Dental.Modules.Clinical.Services;
using Dental.Modules.Patients.Contracts.Services;
using Finbuckle.MultiTenant.Abstractions;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Dental.Modules.Clinical.Features.v1.TreatmentPlans.CreateTreatmentPlan;

/// <summary>Authors a treatment plan, pricing each item from the catalog unless overridden.</summary>
/// <param name="context">The clinical context.</param>
/// <param name="patientService">Confirms the patient exists in this tenant.</param>
/// <param name="tenantContextAccessor">Supplies the resolved tenant.</param>
public sealed class CreateTreatmentPlanCommandHandler(
    ClinicalDbContext context,
    IPatientService patientService,
    IMultiTenantContextAccessor tenantContextAccessor)
    : ICommandHandler<CreateTreatmentPlanCommand, TreatmentPlanDto>
{
    /// <inheritdoc />
    public async ValueTask<TreatmentPlanDto> Handle(
        CreateTreatmentPlanCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        string tenantId = tenantContextAccessor.MultiTenantContext?.TenantInfo?.Id
            ?? throw new ForbiddenException("No tenant is resolved for this request.");

        if (!await patientService.ExistsAsync(command.PatientId, cancellationToken).ConfigureAwait(false))
        {
            throw NotFoundException.For("Patient", command.PatientId);
        }

        Guid[] procedureIds = [.. command.Items.Select(i => i.ProcedureId).Distinct()];

        Dictionary<Guid, Procedure> procedures = await context.Procedures
            .AsNoTracking()
            .Where(p => procedureIds.Contains(p.Id) && p.IsActive)
            .ToDictionaryAsync(p => p.Id, cancellationToken)
            .ConfigureAwait(false);

        Guid[] missing = [.. procedureIds.Where(id => !procedures.ContainsKey(id))];
        if (missing.Length > 0)
        {
            throw new NotFoundException(
                $"These procedures are not in the active catalog: {string.Join(", ", missing)}.");
        }

        // Every item must be quoted in one currency; a plan totalling mixed currencies is meaningless.
        string[] currencies = [.. procedures.Values.Select(p => p.Currency).Distinct()];
        if (currencies.Length > 1)
        {
            throw new CustomException(
                "A treatment plan cannot mix currencies.",
                currencies,
                System.Net.HttpStatusCode.BadRequest);
        }

        TreatmentPlan plan = new()
        {
            PatientId = command.PatientId,
            ProviderId = command.ProviderId,
            Status = TreatmentPlanStatus.Draft,
            Notes = command.Notes,
            Currency = currencies.Length == 1 ? currencies[0] : "USD",
            TenantId = tenantId,
        };

        foreach (TreatmentPlanItemInput input in command.Items)
        {
            Procedure procedure = procedures[input.ProcedureId];

            plan.Items.Add(new TreatmentPlanItem
            {
                TreatmentPlanId = plan.Id,
                ProcedureId = procedure.Id,
                ProcedureCode = procedure.Code,
                Description = procedure.Description,
                ToothNumber = input.ToothNumber,
                Surfaces = input.Surfaces,
                Fee = input.Fee ?? procedure.DefaultFee,
                TenantId = tenantId,
            });
        }

        context.TreatmentPlans.Add(plan);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return TreatmentPlanService.Map(plan);
    }
}
