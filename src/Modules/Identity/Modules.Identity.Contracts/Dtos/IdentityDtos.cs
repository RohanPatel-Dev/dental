namespace Dental.Modules.Identity.Contracts.Dtos;

/// <summary>A user account.</summary>
/// <param name="Id">User identifier.</param>
/// <param name="Email">Sign-in address.</param>
/// <param name="FirstName">Given name.</param>
/// <param name="LastName">Family name.</param>
/// <param name="PhoneNumber">Contact number.</param>
/// <param name="IsActive">Whether the account may sign in.</param>
/// <param name="EmailConfirmed">Whether the address has been verified.</param>
/// <param name="Roles">Role names held.</param>
/// <param name="CreatedAt">When the account was created.</param>
public sealed record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    bool IsActive,
    bool EmailConfirmed,
    IReadOnlyList<string> Roles,
    DateTimeOffset CreatedAt)
{
    /// <summary>Display name.</summary>
    public string FullName => $"{FirstName} {LastName}".Trim();
}

/// <summary>A role and the permissions it grants.</summary>
/// <param name="Id">Role identifier.</param>
/// <param name="Name">Role name.</param>
/// <param name="Description">What the role is for.</param>
/// <param name="IsBuiltIn">Built in roles cannot be deleted.</param>
/// <param name="Permissions">Permission values granted.</param>
public sealed record RoleDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsBuiltIn,
    IReadOnlyList<string> Permissions);

/// <summary>An issued token pair.</summary>
/// <param name="AccessToken">JWT bearer token.</param>
/// <param name="RefreshToken">Opaque refresh token.</param>
/// <param name="ExpiresAt">When the access token expires.</param>
/// <param name="RefreshTokenExpiresAt">When the refresh token expires.</param>
public sealed record TokenDto(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    DateTimeOffset RefreshTokenExpiresAt);

/// <summary>The signed-in user's own profile plus effective permissions.</summary>
/// <param name="User">The account.</param>
/// <param name="Permissions">Effective permission values, unioned across roles.</param>
/// <param name="TenantId">Tenant the session belongs to.</param>
public sealed record CurrentUserDto(UserDto User, IReadOnlyList<string> Permissions, string TenantId);
