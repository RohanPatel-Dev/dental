using Dental.Modules.Tenancy.Contracts.v1.Tenants.SetTenantStatus;
using FluentValidation;

namespace Dental.Modules.Tenancy.Features.v1.Tenants.SetTenantStatus;

/// <summary>Validates <see cref="SetTenantStatusCommand"/>.</summary>
public sealed class SetTenantStatusCommandValidator : AbstractValidator<SetTenantStatusCommand>
{
    /// <summary>Builds the rules.</summary>
    public SetTenantStatusCommandValidator()
    {
        RuleFor(c => c.Identifier).NotEmpty().MaximumLength(64);

        // Deactivating a practice stops its staff working; the reason ends up in the audit trail.
        RuleFor(c => c.Reason)
            .NotEmpty()
            .MaximumLength(512)
            .When(c => !c.IsActive)
            .WithMessage("A reason is required when deactivating a tenant.");
    }
}
