using Dental.Framework.Caching;
using Dental.Framework.Shared.Caching;
using Dental.Modules.Clinical.Contracts.Dtos;
using Dental.Modules.Clinical.Contracts.v1.Procedures.ListProcedures;
using Dental.Modules.Clinical.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace Dental.Modules.Clinical.Features.v1.Procedures.ListProcedures;

/// <summary>Lists the procedure catalog, cached because it changes rarely and is read constantly.</summary>
/// <param name="context">The clinical context.</param>
/// <param name="cache">Shared cache.</param>
public sealed class ListProceduresQueryHandler(ClinicalDbContext context, HybridCache cache)
    : IQueryHandler<ListProceduresQuery, IReadOnlyList<ProcedureDto>>
{
    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<ProcedureDto>> Handle(
        ListProceduresQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        string tenantId = context.CurrentTenantId ?? string.Empty;

        IReadOnlyList<ProcedureDto> catalog = await cache.GetOrCreateAsync(
                CacheKeys.ClinicalKeys.ProcedureCatalog(tenantId),
                async token =>
                {
                    List<ProcedureDto> procedures = await context.Procedures
                        .AsNoTracking()
                        .OrderBy(p => p.Code)
                        .Select(p => new ProcedureDto(
                            p.Id,
                            p.Code,
                            p.Description,
                            p.Category,
                            p.DefaultFee,
                            p.Currency,
                            p.DefaultDurationMinutes,
                            p.IsActive))
                        .ToListAsync(token)
                        .ConfigureAwait(false);

                    return (IReadOnlyList<ProcedureDto>)procedures;
                },
                tags: [CacheKeys.Tags.Clinical],
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        // Filtering happens after the cache read so that one cached list serves every filter
        // combination, rather than a cache entry per query shape.
        IEnumerable<ProcedureDto> filtered = catalog;

        if (query.Category is { } category)
        {
            filtered = filtered.Where(p => p.Category == category);
        }

        if (query.OnlyActive)
        {
            filtered = filtered.Where(p => p.IsActive);
        }

        return [.. filtered];
    }
}
