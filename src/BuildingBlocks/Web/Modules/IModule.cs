using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;

namespace Dental.Framework.Web.Modules;

/// <summary>
/// The plug-in contract every bounded context implements. A module declares itself with an
/// assembly level <see cref="FshModuleAttribute"/> and the loader calls the three phases in order.
/// </summary>
public interface IModule
{
    /// <summary>
    /// Phase 1: DI registration. Register the module's <c>DbContext</c>, initializer, options,
    /// eventing and health checks here.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    void ConfigureServices(IHostApplicationBuilder builder);

    /// <summary>
    /// Phase 2: middleware. Runs AFTER <c>UseAuthentication</c> and BEFORE authorization, so the
    /// caller is known but authorization has not yet run. Optional.
    /// </summary>
    /// <param name="app">The application builder.</param>
    void ConfigureMiddleware(IApplicationBuilder app)
    {
        _ = app;
    }

    /// <summary>
    /// Phase 3: endpoints and recurring jobs.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <remarks>
    /// Register literal-segment routes BEFORE catch-all <c>{id:guid}</c> routes in the same group,
    /// or the literal route is shadowed and silently never matches.
    /// </remarks>
    void MapEndpoints(IEndpointRouteBuilder endpoints);
}
