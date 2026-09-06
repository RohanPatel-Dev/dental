using Dental.Framework.Eventing.Abstractions;

namespace Dental.Modules.Identity.Contracts.Events;

/// <summary>
/// Raised when a user account is created, so other modules can create their own linked records -
/// a Scheduling provider profile, for instance.
/// </summary>
/// <param name="UserId">The new user.</param>
/// <param name="Email">Sign-in address.</param>
/// <param name="FullName">Display name.</param>
/// <param name="Roles">Role names granted at creation.</param>
public sealed record UserCreatedIntegrationEvent(
    Guid UserId,
    string Email,
    string FullName,
    IReadOnlyList<string> Roles) : IntegrationEvent;

/// <summary>Raised when a user is deactivated, so modules can reassign their outstanding work.</summary>
/// <param name="UserId">The deactivated user.</param>
public sealed record UserDeactivatedIntegrationEvent(Guid UserId) : IntegrationEvent;
