using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Dental.Framework.Shared.Tenancy;
using Dental.Framework.Web.Migrations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;

namespace Dental.Integration.Tests.Fixtures;

/// <summary>
/// The API host wired to a throwaway Postgres, migrated and seeded exactly the way the migrator
/// does it in production.
/// </summary>
/// <remarks>
/// One container and one host for the whole suite: starting Postgres per test class would multiply
/// a 5 second cost by every class for no extra coverage. Tests that write use their own tenant or
/// their own patient rather than relying on a clean database.
/// </remarks>
public sealed class DentalApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>Password the seeded operator administrator is given.</summary>
    public const string AdminPassword = "Integration!Tests1";

    /// <summary>Address of the seeded operator administrator.</summary>
    public const string AdminEmail = "operator@dental.local";

    private readonly string _databaseName = $"dental_it_{Guid.CreateVersion7():N}";
    private readonly string _hangfireDatabaseName = $"dental_it_jobs_{Guid.CreateVersion7():N}";

    // Built inside InitializeAsync, not here: the builder itself probes Docker, so a field
    // initializer would throw in the fixture's constructor on a machine without it - before any
    // test gets the chance to skip.
    private PostgreSqlContainer? _postgres;

    /// <summary>Connection string of the suite's database.</summary>
    public string ConnectionString { get; private set; } = string.Empty;

    private readonly Dictionary<string, string> _tokens = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    private string _server = string.Empty;

    /// <summary>Starts the container, then migrates and seeds it.</summary>
    /// <returns>A task that completes once the API is ready to serve.</returns>
    public async ValueTask InitializeAsync()
    {
        if (!TestDatabase.IsAvailable)
        {
            // Every test in the collection skips itself, so there is nothing to set up. Starting
            // the container here anyway would turn "no database" into 18 identical failures.
            return;
        }

        if (TestDatabase.ExternalServer is { } externalServer)
        {
            _server = externalServer;
        }
        else
        {
            _postgres = new PostgreSqlBuilder("postgres:18-alpine")
                .WithDatabase("postgres")
                .WithUsername("dental")
                .WithPassword("dental")
                .Build();

            await _postgres.StartAsync().ConfigureAwait(false);
            _server = _postgres.GetConnectionString();
        }

        // Its own databases, created here and dropped on the way out, so a run never depends on -
        // or damages - anything that was there first. Jobs keep their own logical database, as they
        // do in every other host.
        await CreateDatabaseAsync(_databaseName).ConfigureAwait(false);
        await CreateDatabaseAsync(_hangfireDatabaseName).ConfigureAwait(false);

        ConnectionString = WithDatabase(_databaseName);

        PublishConfiguration();

        // Touching Services forces the host to build, so configuration failures surface here rather
        // than inside the first test.
        _ = Services;

        await MigrateAndSeedAsync().ConfigureAwait(false);
    }

    /// <summary>Creates a client that identifies itself as a tenant.</summary>
    /// <param name="tenant">Tenant identifier sent in the tenant header.</param>
    /// <returns>The client.</returns>
    public HttpClient CreateTenantClient(string tenant = TenantConstants.RootTenant)
    {
        HttpClient client = CreateClient();
        client.DefaultRequestHeaders.Add(TenantConstants.Header, tenant);
        return client;
    }

    /// <summary>Creates a client already carrying a bearer token for the seeded administrator.</summary>
    /// <param name="tenant">Tenant identifier sent in the tenant header.</param>
    /// <returns>The authenticated client.</returns>
    public async Task<HttpClient> CreateAuthenticatedClientAsync(
        string tenant = TenantConstants.RootTenant)
    {
        HttpClient client = CreateTenantClient(tenant);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer",
                await AccessTokenAsync(tenant).ConfigureAwait(false));

        return client;
    }

    /// <summary>Signs the seeded administrator in once per tenant and reuses the token.</summary>
    /// <param name="tenant">Tenant identifier.</param>
    /// <returns>A bearer token.</returns>
    /// <remarks>
    /// Sign-in is rate limited far more tightly than anything else, and every test in the suite
    /// shares one bucket. Reusing the token also matches how a real client behaves.
    /// </remarks>
    public async Task<string> AccessTokenAsync(string tenant = TenantConstants.RootTenant)
    {
        await _tokenLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_tokens.TryGetValue(tenant, out string? cached))
            {
                return cached;
            }

            using HttpClient client = CreateTenantClient(tenant);
            using HttpResponseMessage response = await client
                .PostAsJsonAsync(
                    new Uri("/api/v1/tokens", UriKind.Relative),
                    new { email = AdminEmail, password = AdminPassword })
                .ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            JsonElement token = await response.Content.ReadFromJsonAsync<JsonElement>()
                .ConfigureAwait(false);

            string accessToken = token.GetProperty("accessToken").GetString()
                ?? throw new InvalidOperationException("The token response carried no access token.");

            _tokens[tenant] = accessToken;
            return accessToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ConfigureServices(services =>
        {
            services.AddSingleton<MigrationRunner>();
            services.AddLogging(logging => logging.SetMinimumLevel(LogLevel.Warning));
        });
    }

    /// <summary>
    /// Publishes the host's configuration as environment variables.
    /// </summary>
    /// <remarks>
    /// NOT <c>ConfigureAppConfiguration</c>: the composition root reads several options eagerly
    /// while it registers services, and factory configuration sources are only applied afterwards.
    /// Environment variables are in place before <c>CreateBuilder</c> runs, so they are the only
    /// override the host actually sees. The environment name is one of them - it keeps
    /// appsettings.Development.json, which points at a local Redis, out of the picture.
    /// </remarks>
    private void PublishConfiguration()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "IntegrationTests");
        Environment.SetEnvironmentVariable("DatabaseOptions__ConnectionString", ConnectionString);
        Environment.SetEnvironmentVariable("JobOptions__ConnectionString", HangfireConnectionString());
        Environment.SetEnvironmentVariable("JobOptions__DashboardPassword", "integration-tests");
        Environment.SetEnvironmentVariable("JwtOptions__SigningKey", new string('k', 64));
        Environment.SetEnvironmentVariable("IdentitySeedOptions__AdminPassword", AdminPassword);
        Environment.SetEnvironmentVariable("IdentitySeedOptions__OperatorEmail", AdminEmail);

        // The dispatcher ticks every second so the cross-module tests wait seconds, not minutes.
        Environment.SetEnvironmentVariable("EventingOptions__OutboxDispatchIntervalSeconds", "1");

        // Every request in the suite arrives from the same (absent) address, so the whole run shares
        // one auth bucket. The production limit of 10 would reject the suite rather than test it.
        Environment.SetEnvironmentVariable("RateLimitingOptions__AuthPermitLimit", "500");

        Environment.SetEnvironmentVariable(
            "Storage__LocalRoot",
            Path.Combine(Path.GetTempPath(), "dental-integration-storage"));
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync().ConfigureAwait(false);
        _tokenLock.Dispose();

        if (_postgres is not null)
        {
            // The container goes, and everything in it with it.
            await _postgres.DisposeAsync().ConfigureAwait(false);
        }
        else if (_server.Length > 0)
        {
            await DropDatabaseAsync(_databaseName).ConfigureAwait(false);
            await DropDatabaseAsync(_hangfireDatabaseName).ConfigureAwait(false);
        }
        GC.SuppressFinalize(this);
    }

    private string HangfireConnectionString() =>
        _server.Length == 0 ? string.Empty : WithDatabase(_hangfireDatabaseName);

    private string WithDatabase(string database) =>
        new Npgsql.NpgsqlConnectionStringBuilder(_server) { Database = database }.ConnectionString;

    private async Task CreateDatabaseAsync(string database)
    {
        Npgsql.NpgsqlConnection connection = new(WithDatabase("postgres"));
        await using (connection.ConfigureAwait(false))
        {
            await connection.OpenAsync().ConfigureAwait(false);

            Npgsql.NpgsqlCommand command = connection.CreateCommand();
            await using (command.ConfigureAwait(false))
            {
                // The database name is generated here, never supplied by a caller, so there is
                // nothing to parameterize - and DDL cannot take parameters anyway.
                command.CommandText = string.Create(
                    CultureInfo.InvariantCulture,
                    $"CREATE DATABASE \"{database}\"");
                await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }
    }

    private async Task DropDatabaseAsync(string database)
    {
        Npgsql.NpgsqlConnection connection = new(WithDatabase("postgres"));
        await using (connection.ConfigureAwait(false))
        {
            await connection.OpenAsync().ConfigureAwait(false);

            Npgsql.NpgsqlCommand command = connection.CreateCommand();
            await using (command.ConfigureAwait(false))
            {
                command.CommandText = string.Create(
                    CultureInfo.InvariantCulture,
                    $"DROP DATABASE IF EXISTS \"{database}\" WITH (FORCE)");
                await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }
    }

    private async Task MigrateAndSeedAsync() =>
        await Services.GetRequiredService<MigrationRunner>()
            .RunAsync(
                new MigrationRunOptions(Seed: true, SeedDemo: false, CatalogOnly: false, TenantId: null),
                ConnectionString,
                CancellationToken.None)
            .ConfigureAwait(false);
}
