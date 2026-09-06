namespace Dental.Framework.Shared.Auditing;

/// <summary>Operation names written to the audit trail.</summary>
public static class AuditOperations
{
    /// <summary>A row was inserted.</summary>
    public const string Created = nameof(Created);

    /// <summary>A row was updated.</summary>
    public const string Updated = nameof(Updated);

    /// <summary>A row was soft deleted.</summary>
    public const string Deleted = nameof(Deleted);

    /// <summary>Personal data was read - recorded explicitly, not by the interceptor.</summary>
    public const string Accessed = nameof(Accessed);

    /// <summary>Data left the system.</summary>
    public const string Exported = nameof(Exported);
}

/// <summary>Property names never written to the audit trail or to logs.</summary>
public static class AuditRedaction
{
    /// <summary>Property names whose values are replaced with a placeholder.</summary>
    public static readonly IReadOnlySet<string> SensitiveProperties =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Password",
            "PasswordHash",
            "SecurityStamp",
            "ConcurrencyStamp",
            "RefreshToken",
            "RefreshTokenHash",
            "AccessToken",
            "Secret",
            "ApiKey",
            "SigningKey",
            "SocialSecurityNumber",
            "InsuranceMemberId",
        };

    /// <summary>Placeholder written in place of a sensitive value.</summary>
    public const string Placeholder = "***";
}
