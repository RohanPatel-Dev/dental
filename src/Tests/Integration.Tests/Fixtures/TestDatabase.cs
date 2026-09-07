namespace Dental.Integration.Tests.Fixtures;

/// <summary>
/// Where the suite's Postgres comes from, and whether there is one at all.
/// </summary>
/// <remarks>
/// Normally Testcontainers starts one. Setting <c>DENTAL_TEST_POSTGRES</c> to an admin connection
/// string points the suite at an already-running server instead - useful on a build agent that has
/// Postgres but no container runtime. Either way the fixture creates and drops its own databases,
/// so a run never touches an existing one.
/// </remarks>
public static class TestDatabase
{
    /// <summary>Environment variable naming an existing Postgres server to use.</summary>
    public const string ExternalServerVariable = "DENTAL_TEST_POSTGRES";

    /// <summary>Reason shown on a skipped test.</summary>
    public const string SkipReason =
        "No Postgres is available: start a container runtime, or set DENTAL_TEST_POSTGRES.";

    /// <summary>Connection string of an externally supplied server, when one is configured.</summary>
    public static string? ExternalServer
    {
        get
        {
            string? value = Environment.GetEnvironmentVariable(ExternalServerVariable);
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }

    /// <summary>True when the suite has a database to run against.</summary>
    public static bool IsAvailable => ExternalServer is not null || DockerEnvironment.IsAvailable;
}
