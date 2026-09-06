using Dental.Modules.Identity.Contracts.Dtos;
using Mediator;

namespace Dental.Modules.Identity.Contracts.v1.Users.CreateUser;

/// <summary>Creates a user in the current tenant.</summary>
/// <param name="Email">Sign-in address.</param>
/// <param name="FirstName">Given name.</param>
/// <param name="LastName">Family name.</param>
/// <param name="Password">Initial password.</param>
/// <param name="PhoneNumber">Contact number.</param>
/// <param name="Roles">Roles to grant.</param>
public sealed record CreateUserCommand(
    string Email,
    string FirstName,
    string LastName,
    string Password,
    string? PhoneNumber,
    IReadOnlyList<string> Roles) : ICommand<UserDto>;
