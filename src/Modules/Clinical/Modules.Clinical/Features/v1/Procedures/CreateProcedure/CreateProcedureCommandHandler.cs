using Dental.Framework.Core.Exceptions;
using Dental.Framework.Shared.Caching;
using Dental.Modules.Clinical.Contracts.Dtos;
using Dental.Modules.Clinical.Contracts.v1.Procedures.CreateProcedure;
using Dental.Modules.Clinical.Data;
using Dental.Modules.Clinical.Domain;
using Finbuckle.MultiTenant.Abstractions;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace Dental.Modules.Clinical.Features.v1.Procedures.CreateProcedure;

/// <summary>Adds a procedure to the catalog.</summary>
/// <param name="context">The clinical context.</param>
/// <param name="cache">Shared cache, invalidated after the write.</param>
/// <param name="tenantContextAccessor">Supplies the resolved tenant.</param>
public sealed class CreateProcedureCommandHandler(
    ClinicalDbContext context,
    HybridCache cache,
    IMultiTenantContextAccessor tenantContextAccessor)
    : ICommandHandler<CreateProcedureCommand, ProcedureDto>
{
    /// <inheritdoc />
    public async ValueTask<ProcedureDto> Handle(
        CreateProcedureCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        string tenantId = tenantContextAccessor.MultiTenantContext?.TenantInfo?.Id
            ?? throw new ForbiddenException("No tenant is resolved for this request.");

        string code = command.Code.Trim().ToUpperInvariant();

        bool taken = await context.Procedures
            .AnyAsync(p => p.Code == code, cancellationToken)
            .ConfigureAwait(false);

        if (taken)
        {
            throw new ConflictException($"Procedure code '{code}' is already in the catalog.");
        }

        Procedure procedure = new()
        {
            Code = code,
            Description = command.Description.Trim(),
            Category = command.Category,
            DefaultFee = command.DefaultFee,
            Currency = command.Currency.ToUpperInvariant(),
            DefaultDurationMinutes = command.DefaultDurationMinutes,
            IsActive = true,
            TenantId = tenantId,
        };

        context.Procedures.Add(procedure);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await cache.RemoveByTagAsync(CacheKeys.Tags.Clinical, cancellationToken).ConfigureAwait(false);

        return new ProcedureDto(
            procedure.Id,
            procedure.Code,
            procedure.Description,
            procedure.Category,
            procedure.DefaultFee,
            procedure.Currency,
            procedure.DefaultDurationMinutes,
            procedure.IsActive);
    }
}
