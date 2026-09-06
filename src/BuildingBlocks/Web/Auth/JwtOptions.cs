using System.ComponentModel.DataAnnotations;

namespace Dental.Framework.Web.Auth;

/// <summary>JWT configuration, bound from the <c>JwtOptions</c> section.</summary>
public sealed class JwtOptions : IValidatableObject
{
    /// <summary>Symmetric signing key. At least 32 characters, and never a placeholder.</summary>
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>Token issuer.</summary>
    public string Issuer { get; set; } = "dental";

    /// <summary>Token audience.</summary>
    public string Audience { get; set; } = "dental";

    /// <summary>Access token lifetime in minutes.</summary>
    [Range(1, 1440)]
    public int AccessTokenExpirationMinutes { get; set; } = 60;

    /// <summary>Refresh token lifetime in days.</summary>
    [Range(1, 365)]
    public int RefreshTokenExpirationDays { get; set; } = 7;

    /// <summary>Clock skew allowed when validating expiry, in seconds.</summary>
    [Range(0, 300)]
    public int ClockSkewSeconds { get; set; } = 30;

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(SigningKey) || SigningKey.Length < 32)
        {
            yield return new ValidationResult(
                "JwtOptions:SigningKey must be at least 32 characters.",
                [nameof(SigningKey)]);
        }

        // A key that still contains the scaffolded placeholder is worse than a missing one: it looks
        // configured and is identical across every deployment that copied the sample.
        if (SigningKey.Contains("replace-with", StringComparison.OrdinalIgnoreCase))
        {
            yield return new ValidationResult(
                "JwtOptions:SigningKey still contains the placeholder text 'replace-with'.",
                [nameof(SigningKey)]);
        }
    }
}
