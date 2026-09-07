using Dental.Framework.Web.Auth;
using Dental.Modules.Scheduling.Contracts.Authorization;
using Dental.Modules.Scheduling.Contracts.Dtos;
using Dental.Modules.Scheduling.Contracts.v1.Providers.CreateProvider;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dental.Modules.Scheduling.Features.v1.Providers.CreateProvider;

/// <summary>Maps <c>POST /providers</c>.</summary>
public static class CreateProviderEndpoint
{
    /// <summary>Registers the endpoint.</summary>
    /// <param name="endpoints">The route group.</param>
    /// <returns>The route handler builder, for further configuration.</returns>
    internal static RouteHandlerBuilder MapCreateProviderEndpoint(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost(
                "/providers",
                (
                    CreateProviderCommand command,
                    IMediator mediator,
                    CancellationToken cancellationToken) =>
                    mediator.Send(command, cancellationToken))
            .WithName("CreateProvider")
            .WithSummary("Add a provider to the practice")
            .Produces<ProviderDto>(StatusCodes.Status200OK)
            .RequirePermission(SchedulingPermissions.Providers.Create);
}
