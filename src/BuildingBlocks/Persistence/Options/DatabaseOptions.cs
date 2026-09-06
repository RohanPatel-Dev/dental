using System.ComponentModel.DataAnnotations;

namespace Dental.Framework.Persistence.Options;

/// <summary>
/// Database configuration, bound from the <c>DatabaseOptions</c> section.
/// </summary>
/// <remarks>
/// <see cref="MigrationsAssembly"/> is resolved once per process, so a host that owns its own
/// migrations project must be its own process with its own migrator.
/// </remarks>
public sealed class DatabaseOptions : IValidatableObject
{
    /// <summary>Npgsql connection string for the application database.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Assembly holding the EF migrations this process should apply.</summary>
    public string MigrationsAssembly { get; set; } = "Dental.Migrations.PostgreSQL";

    /// <summary>Seconds before a command times out.</summary>
    [Range(1, 600)]
    public int CommandTimeoutSeconds { get; set; } = 30;

    /// <summary>Enables EF's parameter and query logging. Never enable outside development.</summary>
    public bool EnableSensitiveDataLogging { get; set; }

    /// <summary>Enables EF's detailed error messages.</summary>
    public bool EnableDetailedErrors { get; set; }

    /// <summary>Number of transient failures to retry before giving up.</summary>
    [Range(0, 10)]
    public int MaxRetryCount { get; set; } = 3;

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            yield return new ValidationResult(
                "DatabaseOptions:ConnectionString must be set.",
                [nameof(ConnectionString)]);
        }

        if (string.IsNullOrWhiteSpace(MigrationsAssembly))
        {
            yield return new ValidationResult(
                "DatabaseOptions:MigrationsAssembly must be set.",
                [nameof(MigrationsAssembly)]);
        }
    }
}
