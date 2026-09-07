using Dental.Framework.Core.Exceptions;
using Dental.Framework.Eventing.Outbox;
using Dental.Framework.Quota;
using Dental.Framework.Shared.Caching;
using Dental.Modules.Identity.Contracts.Dtos;
using Dental.Modules.Identity.Contracts.Events;
using Dental.Modules.Identity.Contracts.v1.Users.CreateUser;
using Dental.Modules.Identity.Data;
using Dental.Modules.Identity.Domain;
using Dental.Modules.Identity.Services;
using Finbuckle.MultiTenant.Abstractions;
using Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Hybrid;

namespace Dental.Modules.Identity.Features.v1.Users.CreateUser;

/// <summary>Creates a user in the current tenant and grants their initial roles.</summary>
/// <param name="userManager">ASP.NET Identity user manager.</param>
/// <param name="context">The identity context.</param>
/// <param name="outbox">Outbox writer.</param>
/// <param name="quotaService">Enforces the tenant's user limit.</param>
/// <param name="tenantContextAccessor">Supplies the resolved tenant.</param>
/// <param name="cache">Shared cache, invalidated after the write.</param>
public sealed class CreateUserCommandHandler(
    UserManager<DentalUser> userManager,
    IdentityModuleDbContext context,
    IOutboxStore<IdentityModuleDbContext> outbox,
    IQuotaService quotaService,
    IMultiTenantContextAccessor tenantContextAccessor,
    HybridCache cache) : ICommandHandler<CreateUserCommand, UserDto>
{
    /// <inheritdoc />
    public async ValueTask<UserDto> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        string tenantId = tenantContextAccessor.MultiTenantContext?.TenantInfo?.Id
            ?? throw new ForbiddenException("No tenant is resolved for this request.");

        QuotaUsage usage = await quotaService
            .GetUsageAsync(tenantId, QuotaResource.Users, cancellationToken)
            .ConfigureAwait(false);

        if (usage.IsExceeded)
        {
            throw new CustomException(
                $"This practice has reached its limit of {usage.Limit} users.",
                System.Net.HttpStatusCode.TooManyRequests);
        }

        DentalUser user = new()
        {
            Id = Guid.CreateVersion7(),
            UserName = command.Email.Trim(),
            Email = command.Email.Trim(),
            FirstName = command.FirstName.Trim(),
            LastName = command.LastName.Trim(),
            PhoneNumber = command.PhoneNumber,
            IsActive = true,
            TenantId = tenantId,
        };

        IdentityResult created = await userManager.CreateAsync(user, command.Password)
            .ConfigureAwait(false);

        if (!created.Succeeded)
        {
            throw new CustomException(
                "The user could not be created.",
                [.. created.Errors.Select(e => e.Description)],
                System.Net.HttpStatusCode.BadRequest);
        }

        if (command.Roles.Count > 0)
        {
            IdentityResult granted = await userManager.AddToRolesAsync(user, command.Roles)
                .ConfigureAwait(false);

            if (!granted.Succeeded)
            {
                throw new CustomException(
                    "The roles could not be granted.",
                    [.. granted.Errors.Select(e => e.Description)],
                    System.Net.HttpStatusCode.BadRequest);
            }
        }

        await outbox.AddAsync(
                new UserCreatedIntegrationEvent(user.Id, user.Email, user.FullName, command.Roles)
                {
                    TenantId = tenantId,
                    Source = nameof(Identity),
                },
                cancellationToken)
            .ConfigureAwait(false);

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await cache.RemoveByTagAsync(CacheKeys.Tags.Identity, cancellationToken).ConfigureAwait(false);

        return UserService.Map(user, command.Roles);
    }
}
