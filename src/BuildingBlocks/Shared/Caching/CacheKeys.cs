using System.Globalization;

namespace Dental.Framework.Shared.Caching;

/// <summary>
/// Every cache key and cache tag in the system. Nothing may build a key inline - a typo in an
/// invalidation string is invisible until data goes stale in production.
/// </summary>
public static class CacheKeys
{
    /// <summary>Cache tags. Pass to <c>HybridCache</c> and invalidate with <c>RemoveByTagAsync</c>.</summary>
    public static class Tags
    {
        /// <summary>Everything derived from the tenant catalog.</summary>
        public const string Tenants = "tenants";

        /// <summary>Everything derived from users, roles or permissions.</summary>
        public const string Identity = "identity";

        /// <summary>Everything derived from patient records.</summary>
        public const string Patients = "patients";

        /// <summary>Everything derived from the appointment book.</summary>
        public const string Scheduling = "scheduling";

        /// <summary>Everything derived from the procedure catalog.</summary>
        public const string Clinical = "clinical";

        /// <summary>Everything derived from invoices and payments.</summary>
        public const string Billing = "billing";
    }

    /// <summary>Keys for tenant catalog entries.</summary>
    public static class TenantKeys
    {
        /// <summary>A single tenant by identifier.</summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <returns>The cache key.</returns>
        public static string ById(string tenantId) => $"tenant:{tenantId}";

        /// <summary>The full tenant list.</summary>
        public const string List = "tenant:list";
    }

    /// <summary>Keys for identity data.</summary>
    public static class IdentityKeys
    {
        /// <summary>The effective permission set of one user.</summary>
        /// <param name="userId">User identifier.</param>
        /// <returns>The cache key.</returns>
        public static string UserPermissions(Guid userId) => $"identity:user:{userId}:permissions";

        /// <summary>The claims of one role.</summary>
        /// <param name="roleId">Role identifier.</param>
        /// <returns>The cache key.</returns>
        public static string RolePermissions(Guid roleId) => $"identity:role:{roleId}:permissions";
    }

    /// <summary>Keys for the clinical procedure catalog.</summary>
    public static class ClinicalKeys
    {
        /// <summary>The whole procedure catalog for a tenant.</summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <returns>The cache key.</returns>
        public static string ProcedureCatalog(string tenantId) => $"clinical:{tenantId}:procedures";
    }

    /// <summary>Keys for the appointment book.</summary>
    public static class SchedulingKeys
    {
        /// <summary>One provider's day.</summary>
        /// <param name="providerId">Provider identifier.</param>
        /// <param name="day">The day, interpreted in the practice time zone.</param>
        /// <returns>The cache key.</returns>
        public static string ProviderDay(Guid providerId, DateOnly day) =>
            string.Create(CultureInfo.InvariantCulture, $"scheduling:provider:{providerId}:{day:yyyy-MM-dd}");
    }
}
