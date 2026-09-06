namespace Dental.Framework.Shared.Identity;

/// <summary>Claim types, role names and header names the identity module and the SPAs agree on.</summary>
public static class DentalClaims
{
    /// <summary>Claim carrying a single fine grained permission value.</summary>
    public const string Permission = "permission";

    /// <summary>Claim carrying the tenant the token was issued for.</summary>
    public const string Tenant = "tenant";

    /// <summary>Claim carrying the client application the token was issued to.</summary>
    public const string App = "app";

    /// <summary>Claim set when the session is an impersonation of another user.</summary>
    public const string ImpersonatedBy = "impersonated_by";

    /// <summary>Claim carrying the user's full name.</summary>
    public const string FullName = "full_name";

    /// <summary>Claim carrying the identifier of the issued refresh token family.</summary>
    public const string SessionId = "sid";
}

/// <summary>Built in role names seeded for every tenant.</summary>
public static class DentalRoles
{
    /// <summary>Full control inside a tenant.</summary>
    public const string Admin = nameof(Admin);

    /// <summary>Clinical staff - dentists and hygienists.</summary>
    public const string Clinician = nameof(Clinician);

    /// <summary>Front desk - scheduling and billing, no clinical authoring.</summary>
    public const string FrontDesk = nameof(FrontDesk);

    /// <summary>Read only access.</summary>
    public const string Viewer = nameof(Viewer);

    /// <summary>Every seeded role name.</summary>
    public static readonly IReadOnlyList<string> All = [Admin, Clinician, FrontDesk, Viewer];
}

/// <summary>Client applications that may request a token.</summary>
public static class DentalApps
{
    /// <summary>The operator console.</summary>
    public const string Admin = "admin";

    /// <summary>The tenant facing dashboard.</summary>
    public const string Dashboard = "dashboard";

    /// <summary>Header naming the requesting application.</summary>
    public const string HeaderName = "X-App";
}
