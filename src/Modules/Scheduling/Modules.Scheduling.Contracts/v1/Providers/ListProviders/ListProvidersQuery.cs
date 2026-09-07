using Dental.Modules.Scheduling.Contracts.Dtos;
using Mediator;

namespace Dental.Modules.Scheduling.Contracts.v1.Providers.ListProviders;

/// <summary>Lists the practice's providers. Bounded, so it is not paginated.</summary>
/// <param name="OnlyAcceptingPatients">Restricts to providers taking new patients.</param>
public sealed record ListProvidersQuery(bool OnlyAcceptingPatients) : IQuery<IReadOnlyList<ProviderDto>>;
