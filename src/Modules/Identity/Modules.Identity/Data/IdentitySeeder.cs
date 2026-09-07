using Dental.Framework.Shared.Identity;
using Dental.Framework.Shared.Tenancy;
using Dental.Modules.Identity.Domain;
using Dental.Modules.Identity.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dental.Modules.Identity.Data;

/// <summary>Creates the built-in roles and the first administrator for a tenant.</summary>
/// <param name="context">The identity context.</param>
/// <param name="userManager">ASP.NET Identity user manager.</param>
/// <param name="seedOptions">Seeding configuration, including the administrator password.</param>
/// <param name="logger">Logger.</param>
public sealed class IdentitySeeder(
    IdentityModuleDbContext context,
    UserManager<DentalUser> userManager,
    IOptions<IdentitySeedOptions> seedOptions,
    ILogger<IdentitySeeder> logger)
{

    /// <summary>Creates the four built-in roles for a tenant, if they are missing.</summary>
    /// <param name="tenantId">Tenant to seed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the roles exist.</returns>
    public async Task SeedRolesAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        HashSet<string> existing = await context.Roles
            .IgnoreQueryFilters()
            .Where(r => r.TenantId == tenantId)
            .Select(r => r.Name!)
            .ToHashSetAsync(StringComparer.Ordinal, cancellationToken)
            .ConfigureAwait(false);

        foreach (string roleName in DentalRoles.All.Where(r => !existing.Contains(r)))
        {
            DentalRole role = new()
            {
                Id = Guid.CreateVersion7(),
                Name = roleName,
                NormalizedName = roleName.ToUpperInvariant(),
                Description = DescribeRole(roleName),
                IsBuiltIn = true,
                TenantId = tenantId,
            };

            context.Roles.Add(role);

            foreach (string permission in PermissionsFor(roleName, tenantId))
            {
                context.RoleClaims.Add(UserService.ToRoleClaim(role.Id, permission));
            }

            logger.LogInformation("Seeded role {RoleName} for tenant {TenantId}.", roleName, tenantId);
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Creates the tenant's first administrator, if it does not already exist.</summary>
    /// <param name="tenantId">Tenant to seed.</param>
    /// <param name="email">Administrator address.</param>
    /// <param name="practiceName">Practice name, used for the display name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the account exists.</returns>
    public async Task SeedAdministratorAsync(
        string tenantId,
        string email,
        string practiceName,
        CancellationToken cancellationToken = default)
    {
        string normalizedEmail = email.ToUpperInvariant();

        bool exists = await context.Users
            .IgnoreQueryFilters()
            .AnyAsync(
                u => u.TenantId == tenantId && u.NormalizedEmail == normalizedEmail,
                cancellationToken)
            .ConfigureAwait(false);

        if (exists)
        {
            return;
        }

        DentalUser admin = new()
        {
            Id = Guid.CreateVersion7(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = "Practice",
            LastName = "Administrator",
            IsActive = true,
            TenantId = tenantId,
        };

        IdentityResult created = await userManager.CreateAsync(admin, seedOptions.Value.AdminPassword)
            .ConfigureAwait(false);

        if (!created.Succeeded)
        {
            logger.LogError(
                "Could not seed the administrator for tenant {TenantId}: {Errors}",
                tenantId,
                string.Join("; ", created.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(admin, DentalRoles.Admin).ConfigureAwait(false);

        logger.LogWarning(
            "Seeded administrator {Email} for {PracticeName} with the default password. "
            + "It must be changed before the practice goes live.",
            email,
            practiceName);
    }

    private static string DescribeRole(string roleName) => roleName switch
    {
        DentalRoles.Admin => "Full control of the practice, including staff and billing.",
        DentalRoles.Clinician => "Dentists and hygienists: charting, treatment plans and notes.",
        DentalRoles.FrontDesk => "Scheduling, patient records and payments; no clinical authoring.",
        DentalRoles.Viewer => "Read only access.",
        _ => "Custom role.",
    };

    /// <remarks>
    /// The root tenant's administrator is the only account that gets the root-only permissions -
    /// the tenant catalog is administered from there. Without this, the operator permissions are
    /// declared by Tenancy but granted to nobody, and no practice can ever be created through the
    /// API.
    /// </remarks>
    private static IEnumerable<string> PermissionsFor(string roleName, string tenantId) => roleName switch
    {
        DentalRoles.Admin when string.Equals(tenantId, TenantConstants.RootTenant, StringComparison.Ordinal) =>
            PermissionConstants.All.Select(p => p.Value),

        DentalRoles.Admin => PermissionConstants.Admin.Select(p => p.Value),

        DentalRoles.Clinician => PermissionConstants.Admin
            .Where(p => p.Group is "Patients" or "Clinical" or "Scheduling")
            .Select(p => p.Value),

        DentalRoles.FrontDesk => PermissionConstants.Admin
            .Where(p => p.Group is "Patients" or "Scheduling" or "Billing")
            .Where(p => p.Action != PermissionActions.Delete)
            .Select(p => p.Value),

        DentalRoles.Viewer => PermissionConstants.Admin
            .Where(p => p.Action is PermissionActions.View or PermissionActions.Search)
            .Select(p => p.Value),

        _ => [],
    };
}
