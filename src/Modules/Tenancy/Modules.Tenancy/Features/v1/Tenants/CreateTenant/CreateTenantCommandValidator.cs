using System.Text.RegularExpressions;
using Dental.Modules.Tenancy.Contracts.v1.Tenants.CreateTenant;
using FluentValidation;

namespace Dental.Modules.Tenancy.Features.v1.Tenants.CreateTenant;

/// <summary>Validates <see cref="CreateTenantCommand"/>.</summary>
public sealed partial class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    /// <summary>Builds the rules.</summary>
    public CreateTenantCommandValidator()
    {
        RuleFor(c => c.Identifier)
            .NotEmpty()
            .MaximumLength(64)
            .Must(id => Slug().IsMatch(id))
            .WithMessage("Identifier must be lower case letters, digits and hyphens.");

        RuleFor(c => c.Name).NotEmpty().MaximumLength(256);
        RuleFor(c => c.AdminEmail).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(c => c.Plan).NotEmpty().MaximumLength(64);

        RuleFor(c => c.TimeZone)
            .NotEmpty()
            .Must(BeAKnownTimeZone)
            .WithMessage("TimeZone must be a known IANA time zone identifier.");

        RuleFor(c => c.ValidUntil)
            .Must(v => v is null || v > DateTimeOffset.UtcNow)
            .WithMessage("ValidUntil must be in the future.");
    }

    private static bool BeAKnownTimeZone(string timeZone) =>
        !string.IsNullOrWhiteSpace(timeZone) && TimeZoneInfo.TryFindSystemTimeZoneById(timeZone, out _);

    [GeneratedRegex("^[a-z0-9][a-z0-9-]{1,62}[a-z0-9]$")]
    private static partial Regex Slug();
}
