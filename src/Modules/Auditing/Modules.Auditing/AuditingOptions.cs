using System.ComponentModel.DataAnnotations;

namespace Dental.Modules.Auditing;

/// <summary>Auditing configuration, bound from the <c>AuditingOptions</c> section.</summary>
public sealed class AuditingOptions
{
    /// <summary>
    /// Days an audit row is kept. Health record legislation usually sets a floor here, so treat
    /// lowering it as a compliance decision rather than a tuning knob.
    /// </summary>
    [Range(30, 3650)]
    public int RetentionDays { get; set; } = 2555;

    /// <summary>Cron expression for the retention sweep. Defaults to daily.</summary>
    public string RetentionCron { get; set; } = "0 3 * * *";
}
