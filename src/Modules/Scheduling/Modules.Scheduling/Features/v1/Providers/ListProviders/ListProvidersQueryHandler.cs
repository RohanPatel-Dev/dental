using Dental.Modules.Scheduling.Contracts.Dtos;
using Dental.Modules.Scheduling.Contracts.v1.Providers.ListProviders;
using Dental.Modules.Scheduling.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Dental.Modules.Scheduling.Features.v1.Providers.ListProviders;

/// <summary>Lists the practice's providers.</summary>
/// <param name="context">The scheduling context.</param>
public sealed class ListProvidersQueryHandler(SchedulingDbContext context)
    : IQueryHandler<ListProvidersQuery, IReadOnlyList<ProviderDto>>
{
    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<ProviderDto>> Handle(
        ListProvidersQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        IQueryable<Domain.Provider> providers = context.Providers.AsNoTracking();

        if (query.OnlyAcceptingPatients)
        {
            providers = providers.Where(p => p.IsAcceptingPatients);
        }

        List<ProviderDto> results = await providers
            .OrderBy(p => p.DisplayName)
            .Select(p => new ProviderDto(p.Id, p.UserId, p.DisplayName, p.Speciality, p.IsAcceptingPatients))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return results;
    }
}
