using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dental.Integration.Middleware.Tests;

/// <summary>
/// A throwaway in-memory host holding exactly the middleware under test.
/// </summary>
/// <remarks>
/// Deliberately not the API host: these tests assert the behaviour of individual components and
/// their order, so anything else in the pipeline is noise that would make a failure ambiguous.
/// </remarks>
public sealed class PipelineHost : IAsyncDisposable
{
    private readonly WebApplication _app;

    private PipelineHost(WebApplication app)
    {
        _app = app;
        Client = app.GetTestClient();
    }

    /// <summary>A client bound to the in-memory server.</summary>
    public HttpClient Client { get; }

    /// <summary>Builds and starts a host.</summary>
    /// <param name="configureServices">Registers whatever the middleware under test resolves.</param>
    /// <param name="configure">Composes the pipeline and maps the endpoints.</param>
    /// <returns>The running host.</returns>
    public static async Task<PipelineHost> StartAsync(
        Action<IServiceCollection> configureServices,
        Action<WebApplication> configure)
    {
        ArgumentNullException.ThrowIfNull(configureServices);
        ArgumentNullException.ThrowIfNull(configure);

        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        builder.Environment.EnvironmentName = Environments.Development;
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();

        configureServices(builder.Services);

        WebApplication app = builder.Build();
        configure(app);

        await app.StartAsync().ConfigureAwait(false);

        return new PipelineHost(app);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _app.DisposeAsync().ConfigureAwait(false);
    }
}
