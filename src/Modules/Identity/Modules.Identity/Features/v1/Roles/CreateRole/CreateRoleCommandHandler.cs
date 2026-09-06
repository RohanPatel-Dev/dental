using Dental.Framework.Core.Exceptions;
using Dental.Framework.Shared.Caching;
using Dental.Modules.Identity.Contracts.Dtos;
using Dental.Modules.Identity.Contracts.v1.Roles.CreateRole;
using Dental.Modules.Identity.Data;
using Dental.Modules.Identity.Domain;
using Dental.Modules.Identity.Services;
using Finbuckle.MultiTenant.Abstractions;
using Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Hybrid;

namespace Dental.Modules.Identity.Features.v1.Roles.CreateRole;

/// <summary>Creates a role and grants it the requested permissions.</summary>
/// <param name="roleManager">ASP.NET Identity role manager.</param>
/// <param name="context">The identity context.</param>
/// <param name="tenantContextAccessor">Supplies the resolved tenant.</param>
/// <param name="cache">Shared cache, invalidated after the write.</param>
public sealed class CreateRoleCommandHandler(
    RoleManager<DentalRole> roleManager,
    IdentityModuleDbContext context,
    IMultiTenantContextAccessor tenantContextAccessor,
    HybridCache cache) : ICommandHandler<CreateRoleCommand, RoleDto>
{
    /// <inheritdoc />
    public async ValueTask<RoleDto> Handle(CreateRoleCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        string tenantId = tenantContextAccessor.MultiTenantContext?.TenantInfo?.Id
            ?? throw new ForbiddenException("No tenant is resolved for this request.");

        DentalRole role = new()
        {
            Id = Guid.CreateVersion7(),
            Name = command.Name.Trim(),
            Description = command.Description,
            IsBuiltIn = false,
            TenantId = tenantId,
        };

        IdentityResult created = await roleManager.CreateAsync(role).ConfigureAwait(false);
        if (!created.Succeeded)
        {
            throw new CustomException(
                "The role could not be created.",
                [.. created.Errors.Select(e => e.Description)],
                System.Net.HttpStatusCode.BadRequest);
        }

        foreach (string permission in command.Permissions.Distinct(StringComparer.Ordinal))
        {
            context.RoleClaims.Add(UserService.ToRoleClaim(role.Id, permission));
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await cache.RemoveByTagAsync(CacheKeys.Tags.Identity, cancellationToken).ConfigureAwait(false);

        return new RoleDto(role.Id, role.Name, role.Description, role.IsBuiltIn, command.Permissions);
    }
}
