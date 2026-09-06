using System.ComponentModel.DataAnnotations;

namespace Dental.Framework.Jobs;

/// <summary>Background job configuration, bound from the <c>JobOptions</c> section.</summary>
public sealed class JobOptions
{
    /// <summary>
    /// Connection string for this host's Hangfire storage.
    /// </summary>
    /// <remarks>
    /// Two hosts must NEVER share Hangfire storage. Every host's Hangfire server polls the same
    /// recurring job table, and a host that cannot resolve a job's type disables that job
    /// GLOBALLY - including for the host that could have run it. Give each host its own logical
    /// Hangfire database.
    /// </remarks>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Path the dashboard is mounted at.</summary>
    public string DashboardPath { get; set; } = "/jobs";

    /// <summary>Dashboard basic auth user name.</summary>
    public string DashboardUser { get; set; } = "admin";

    /// <summary>
    /// Dashboard basic auth password. Required and at least 12 characters, so a host outside
    /// Development fails to start rather than exposing the dashboard.
    /// </summary>
    [Required]
    [MinLength(12)]
    public string DashboardPassword { get; set; } = string.Empty;

    /// <summary>Worker count. Defaults to the processor count when left at zero.</summary>
    [Range(0, 200)]
    public int WorkerCount { get; set; }

    /// <summary>Queues this host's server processes, in priority order.</summary>
    public IList<string> Queues { get; } = [JobQueues.Default, JobQueues.Email];

    /// <summary>Days a succeeded job is retained.</summary>
    [Range(1, 365)]
    public int RetentionDays { get; set; } = 7;
}

/// <summary>Queue names. Anything long running or rate limited gets its own queue.</summary>
public static class JobQueues
{
    /// <summary>General purpose queue.</summary>
    public const string Default = "default";

    /// <summary>Outbound mail, kept separate so a slow SMTP server cannot starve everything else.</summary>
    public const string Email = "email";
}
