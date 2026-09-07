using Dental.Framework.Web.Auth;
using Dental.Modules.Scheduling.Contracts.Authorization;
using Dental.Modules.Scheduling.Contracts.Dtos;
using Dental.Modules.Scheduling.Contracts.v1.Providers.ListProviders;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dental.Modules.Scheduling.Features.v1.Providers.ListProviders;

/// <summary>Maps <c>GET /providers</c>.</summary>
public static class ListProvidersEndpoint
{
    /// <summary>Registers the endpoint.</summary>
    /// <param name="endpoints">The route group.</param>
    /// <returns>The route handler builder, for further configuration.</returns>
    internal static RouteHandlerBuilder MapListProvidersEndpoint(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet(
                "/providers",
                (
                    bool? onlyAcceptingPatients,
                    IMediator mediator,
                    CancellationToken cancellationToken) =>
                    mediator.Send(
                        new ListProvidersQuery(onlyAcceptingPatients ?? false),
                        cancellationToken))
            .WithName("ListProviders")
            .WithSummary("List the practice's providers")
            .Produces<IReadOnlyList<ProviderDto>>(StatusCodes.Status200OK)
            .RequirePermission(SchedulingPermissions.Providers.View);
}
