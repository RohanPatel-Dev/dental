using Dental.Framework.Quota;
using Dental.Modules.Identity.Contracts.Services;

namespace Dental.Modules.Identity.Services;

/// <summary>Reports the tenant's active user count to the quota subsystem.</summary>
/// <param name="userService">Counts active users.</param>
public sealed class UserQuotaGaugeProvider(IUserService userService) : IQuotaGaugeProvider
{
    /// <inheritdoc />
    public QuotaResource Resource => QuotaResource.Users;

    /// <inheritdoc />
    public Task<long> GetCurrentAsync(string tenantId, CancellationToken cancellationToken = default) =>
        userService.CountActiveUsersAsync(tenantId, cancellationToken);
}
