using Dental.Framework.Shared.Tenancy;
using Dental.Framework.Web.Tenancy;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Dental.Framework.Web.Middleware;

/// <summary>
/// Rejects a request that names a tenant which does not exist.
/// </summary>
/// <remarks>
/// Finbuckle's strategies are a chain: when the header names a tenant the store cannot find, it
/// falls through to the static strategy and the request silently becomes a ROOT tenant request.
/// That fallback is wanted when no tenant was named at all (the operator app, health probes, the
/// token endpoint before sign-in) but not when the caller named one - answering a request for
/// "some-practice" with root's data is exactly the isolation failure the whole design exists to
/// prevent. This middleware runs immediately after <c>UseMultiTenant()</c>, before authentication.
/// </remarks>
/// <param name="next">Next middleware.</param>
public sealed class UnknownTenantMiddleware(RequestDelegate next)
{
    /// <summary>Runs the middleware.</summary>
    /// <param name="context">The request.</param>
    /// <returns>A task that completes when the pipeline has run.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        string? requested = Requested(context);

        if (requested is not null && !Resolves(context, requested))
        {
            ProblemDetails problem = new()
            {
                Title = "Unknown tenant.",
                Detail = $"No practice is registered under '{requested}'.",
                Status = StatusCodes.Status400BadRequest,
            };

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response
                .WriteAsJsonAsync(problem, options: null, "application/problem+json", context.RequestAborted)
                .ConfigureAwait(false);

            return;
        }

        await next(context).ConfigureAwait(false);
    }

    private static string? Requested(HttpContext context)
    {
        if (context.Request.Headers[TenantConstants.Header].ToString() is { Length: > 0 } header)
        {
            return header;
        }

        return context.Request.Query[TenantConstants.QueryStringKey].ToString() is { Length: > 0 } query
            ? query
            : null;
    }

    private static bool Resolves(HttpContext context, string requested)
    {
        IMultiTenantContextAccessor<DentalTenantInfo>? accessor =
            context.RequestServices.GetService(typeof(IMultiTenantContextAccessor<DentalTenantInfo>))
                as IMultiTenantContextAccessor<DentalTenantInfo>;

        DentalTenantInfo? resolved = accessor?.MultiTenantContext.TenantInfo;

        return resolved is not null
            && string.Equals(resolved.Identifier, requested, StringComparison.OrdinalIgnoreCase);
    }
}
