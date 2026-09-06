using Dental.Modules.Tenancy.Contracts.v1.Tenants.GetTenant;
using FluentValidation;

namespace Dental.Modules.Tenancy.Features.v1.Tenants.GetTenant;

/// <summary>Validates <see cref="GetTenantQuery"/>.</summary>
public sealed class GetTenantQueryValidator : AbstractValidator<GetTenantQuery>
{
    /// <summary>Builds the rules.</summary>
    public GetTenantQueryValidator() =>
        RuleFor(q => q.Identifier).NotEmpty().MaximumLength(64);
}
