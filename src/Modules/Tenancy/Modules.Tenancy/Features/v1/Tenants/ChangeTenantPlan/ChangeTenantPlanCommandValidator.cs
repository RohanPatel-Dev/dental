using Dental.Modules.Tenancy.Contracts.v1.Tenants.ChangeTenantPlan;
using FluentValidation;

namespace Dental.Modules.Tenancy.Features.v1.Tenants.ChangeTenantPlan;

/// <summary>Validates <see cref="ChangeTenantPlanCommand"/>.</summary>
public sealed class ChangeTenantPlanCommandValidator : AbstractValidator<ChangeTenantPlanCommand>
{
    /// <summary>Builds the rules.</summary>
    public ChangeTenantPlanCommandValidator()
    {
        RuleFor(c => c.Identifier).NotEmpty().MaximumLength(64);
        RuleFor(c => c.Plan).NotEmpty().MaximumLength(64);
    }
}
