using System.Globalization;

// The whole stack with one command:
//
//   dotnet run --project src/Host/Dental.AppHost
//
// Postgres + pgAdmin, Valkey + RedisInsight, MinIO, RabbitMQ, the migrators, both API hosts and
// both React apps.
IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

// Secrets are parameters, never literals: `dotnet user-secrets set Parameters:postgres-password ...`
IResourceBuilder<ParameterResource> postgresPassword =
    builder.AddParameter("postgres-password", secret: true);
IResourceBuilder<ParameterResource> jwtSigningKey =
    builder.AddParameter("jwt-signing-key", secret: true);
IResourceBuilder<ParameterResource> jobsDashboardPassword =
    builder.AddParameter("jobs-dashboard-password", secret: true);
IResourceBuilder<ParameterResource> seedAdminPassword =
    builder.AddParameter("seed-admin-password", secret: true);
IResourceBuilder<ParameterResource> minioPassword =
    builder.AddParameter("minio-password", secret: true);

IResourceBuilder<PostgresServerResource> postgres = builder
    .AddPostgres("postgres", password: postgresPassword, port: 5432)
    .WithDataVolume()
    .WithPgAdmin(pgAdmin => pgAdmin.WithHostPort(5050));

IResourceBuilder<PostgresDatabaseResource> applicationDatabase = postgres.AddDatabase("dental");

// One logical Hangfire database PER HOST. Two hosts sharing Hangfire storage both poll the same
// recurring-job table, and a host that cannot resolve a job's type disables that job GLOBALLY -
// including for the host that could have run it.
IResourceBuilder<PostgresDatabaseResource> apiJobsDatabase =
    postgres.AddDatabase("dental-hangfire-api");
IResourceBuilder<PostgresDatabaseResource> notificationsJobsDatabase =
    postgres.AddDatabase("dental-hangfire-notifications");

// Valkey rather than Redis: wire compatible, BSD-3 licensed.
IResourceBuilder<ContainerResource> valkey = builder
    .AddContainer("valkey", "valkey/valkey", "8-alpine")
    .WithEndpoint(port: 6379, targetPort: 6379, name: "tcp", scheme: "tcp");

builder.AddContainer("redisinsight", "redis/redisinsight", "latest")
    .WithHttpEndpoint(port: 5540, targetPort: 5540, name: "http")
    .WaitFor(valkey);

IResourceBuilder<RabbitMQServerResource> rabbitmq = builder
    .AddRabbitMQ("rabbitmq")
    .WithManagementPlugin(port: 15672);

(IResourceBuilder<ContainerResource> minio, string minioEndpoint) = AddMinio(builder, minioPassword);

const string ValkeyConnectionString = "localhost:6379";
const string AdminOrigin = "http://localhost:5173";
const string DashboardOrigin = "http://localhost:5174";

// The migrator runs to completion before the API starts, so the API never meets an unmigrated
// database.
IResourceBuilder<ProjectResource> migrator = builder
    .AddProject<Projects.Dental_DbMigrator>("db-migrator")
    .WithArgs("apply", "--seed")
    .WithReference(applicationDatabase)
    .WaitFor(applicationDatabase)

    // "Minimum Pool Size=5" so the health probes do not cold-open a whole connection cohort at once.
    .WithEnvironment("DatabaseOptions__ConnectionString", BuildConnectionString(applicationDatabase))
    .WithEnvironment("JwtOptions__SigningKey", jwtSigningKey)
    .WithEnvironment("IdentitySeedOptions__AdminPassword", seedAdminPassword);

IResourceBuilder<ProjectResource> api = builder
    .AddProject<Projects.Dental_Api>("api")
    .WithReference(applicationDatabase)
    .WithReference(rabbitmq)
    .WaitForCompletion(migrator)
    .WaitFor(rabbitmq)
    .WithEnvironment("DatabaseOptions__ConnectionString", BuildConnectionString(applicationDatabase))
    .WithEnvironment("JobOptions__ConnectionString", BuildConnectionString(apiJobsDatabase))
    .WithEnvironment("JobOptions__DashboardPassword", jobsDashboardPassword)
    .WithEnvironment("CachingOptions__Redis", ValkeyConnectionString)
    .WithEnvironment("JwtOptions__SigningKey", jwtSigningKey)
    .WithEnvironment("IdentitySeedOptions__AdminPassword", seedAdminPassword)
    .WithEnvironment("EventingOptions__Provider", "RabbitMQ")
    .WithEnvironment("Storage__Provider", "s3")
    .WithEnvironment("Storage__ServiceUrl", minioEndpoint)
    .WithEnvironment("Storage__AccessKey", "dental")
    .WithEnvironment("Storage__SecretKey", minioPassword)
    .WithEnvironment("CorsOptions__AllowedOrigins__0", AdminOrigin)
    .WithEnvironment("CorsOptions__AllowedOrigins__1", DashboardOrigin)
    .WithHttpEndpoint(port: 5030, name: "http")
    .WithHttpsEndpoint(port: 7030, name: "https")
    .WaitFor(minio);

// The extracted host: its OWN migrator, its OWN Hangfire database, and RabbitMQ rather than the
// in-memory bus, which cannot cross a process boundary.
IResourceBuilder<ProjectResource> notificationsMigrator = builder
    .AddProject<Projects.Dental_NotificationsMigrator>("notifications-migrator")
    .WithArgs("apply", "--seed")
    .WithReference(applicationDatabase)
    .WaitForCompletion(migrator)
    .WithEnvironment("DatabaseOptions__ConnectionString", BuildConnectionString(applicationDatabase))
    .WithEnvironment("JwtOptions__SigningKey", jwtSigningKey);

builder.AddProject<Projects.Dental_NotificationsHost>("notifications")
    .WithReference(applicationDatabase)
    .WithReference(rabbitmq)
    .WaitForCompletion(notificationsMigrator)
    .WaitFor(rabbitmq)
    .WithEnvironment("DatabaseOptions__ConnectionString", BuildConnectionString(applicationDatabase))
    .WithEnvironment("JobOptions__ConnectionString", BuildConnectionString(notificationsJobsDatabase))
    .WithEnvironment("JobOptions__DashboardPassword", jobsDashboardPassword)

    // The SAME Redis as the API, so this headless host's IHubContext pushes reach browsers
    // connected there. A different instance here would make every push silently vanish.
    .WithEnvironment("CachingOptions__Redis", ValkeyConnectionString)
    .WithEnvironment("JwtOptions__SigningKey", jwtSigningKey)
    .WithEnvironment("EventingOptions__Provider", "RabbitMQ")
    .WithHttpEndpoint(port: 5040, name: "http");

builder.AddViteApp("admin", "../../../clients/admin")
    .WithHttpEndpoint(port: 5173, env: "PORT")
    .WithEnvironment("VITE_DEV_PROXY_TARGET", "http://localhost:5030")
    .WaitFor(api);

builder.AddViteApp("dashboard", "../../../clients/dashboard")
    .WithHttpEndpoint(port: 5174, env: "PORT")
    .WithEnvironment("VITE_DEV_PROXY_TARGET", "http://localhost:5030")
    .WaitFor(api);

await builder.Build().RunAsync().ConfigureAwait(false);

static string BuildConnectionString(IResourceBuilder<PostgresDatabaseResource> database) =>
    string.Create(
        CultureInfo.InvariantCulture,
        $"{database.Resource.ConnectionStringExpression.ValueExpression};Minimum Pool Size=5");

static (IResourceBuilder<ContainerResource> Container, string Endpoint) AddMinio(
    IDistributedApplicationBuilder builder,
    IResourceBuilder<ParameterResource> password)
{
    IResourceBuilder<ContainerResource> minio = builder
        .AddContainer("minio", "minio/minio", "latest")
        .WithArgs("server", "/data", "--console-address", ":9001")
        .WithEnvironment("MINIO_ROOT_USER", "dental")
        .WithEnvironment("MINIO_ROOT_PASSWORD", password)

        // Lets the browser PUT straight to MinIO with a presigned URL instead of streaming the file
        // through the API.
        .WithEnvironment(
            "MINIO_API_CORS_ALLOW_ORIGIN",
            "http://localhost:5173,http://localhost:5174")
        .WithVolume("dental-minio", "/data")
        .WithHttpEndpoint(port: 9000, targetPort: 9000, name: "api")
        .WithHttpEndpoint(port: 9001, targetPort: 9001, name: "console");

    // The bucket has to exist before the first upload; mc creates it and exits.
    // ReplaceLineEndings("\n") because a CRLF script is not runnable by /bin/sh in the container.
    string initScript = """
        #!/bin/sh
        set -e
        until mc alias set local http://minio:9000 "$MINIO_ROOT_USER" "$MINIO_ROOT_PASSWORD"; do
          echo 'waiting for minio'
          sleep 1
        done
        mc mb --ignore-existing local/dental
        echo 'bucket ready'
        """.ReplaceLineEndings("\n");

    builder.AddContainer("minio-init", "minio/mc", "latest")
        .WithEnvironment("MINIO_ROOT_USER", "dental")
        .WithEnvironment("MINIO_ROOT_PASSWORD", password)
        .WithEntrypoint("/bin/sh")
        .WithArgs("-c", initScript)
        .WaitFor(minio);

    return (minio, "http://localhost:9000");
}
