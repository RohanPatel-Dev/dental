namespace Dental.Framework.Shared.Tenancy;

/// <summary>
/// Tenant resolution facts. Resolution is header driven, not claim driven, because
/// <c>UseMultiTenant()</c> runs before authentication.
/// </summary>
public static class TenantConstants
{
    /// <summary>Header the SPAs send to select a tenant. Lower case by convention.</summary>
    public const string Header = "tenant";

    /// <summary>Identifier of the operator ("root") tenant that owns the tenant catalog.</summary>
    public const string RootTenant = "root";

    /// <summary>Query string key accepted as a fallback for the tenant header.</summary>
    public const string QueryStringKey = "tenant";

    /// <summary>Claim consulted after authentication when no header was supplied.</summary>
    public const string ClaimType = "tenant";
}
