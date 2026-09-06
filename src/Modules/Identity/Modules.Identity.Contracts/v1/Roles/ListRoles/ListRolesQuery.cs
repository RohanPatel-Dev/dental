using Dental.Modules.Identity.Contracts.Dtos;
using Mediator;

namespace Dental.Modules.Identity.Contracts.v1.Roles.ListRoles;

/// <summary>Lists the roles of the current tenant. Small and bounded, so it is not paginated.</summary>
public sealed record ListRolesQuery : IQuery<IReadOnlyList<RoleDto>>;
