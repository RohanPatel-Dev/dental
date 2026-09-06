using Dental.Framework.Shared.Identity;
using Dental.Framework.Web.RateLimiting;
using Dental.Modules.Identity.Contracts.Dtos;
using Dental.Modules.Identity.Contracts.v1.Tokens.IssueToken;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;

namespace Dental.Modules.Identity.Features.v1.Tokens.IssueToken;

/// <summary>Maps <c>POST /tokens</c>.</summary>
public static class IssueTokenEndpoint
{
    /// <summary>Registers the endpoint.</summary>
    /// <param name="endpoints">The route group.</param>
    /// <returns>The route handler builder, for further configuration.</returns>
    internal static RouteHandlerBuilder MapIssueTokenEndpoint(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost(
                "/tokens",
                (
                    IssueTokenRequest request,
                    HttpContext httpContext,
                    IMediator mediator,
                    CancellationToken cancellationToken) =>
                    mediator.Send(
                        new IssueTokenCommand(
                            request.Email,
                            request.Password,
                            httpContext.Request.Headers[DentalApps.HeaderName].ToString() is { Length: > 0 } app
                                ? app
                                : DentalApps.Dashboard),
                        cancellationToken))
            .WithName("IssueToken")
            .WithSummary("Exchange credentials for a token pair")
            .Produces<TokenDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitingRegistration.AuthPolicy);
}

/// <summary>Body shape for <see cref="IssueTokenEndpoint"/>.</summary>
/// <param name="Email">Sign-in address.</param>
/// <param name="Password">Password.</param>
public sealed record IssueTokenRequest(string Email, string Password);
