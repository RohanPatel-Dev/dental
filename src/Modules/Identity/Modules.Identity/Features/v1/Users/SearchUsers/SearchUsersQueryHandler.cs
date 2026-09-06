using Dental.Framework.Persistence.Pagination;
using Dental.Framework.Shared.Pagination;
using Dental.Modules.Identity.Contracts.Dtos;
using Dental.Modules.Identity.Contracts.v1.Users.SearchUsers;
using Dental.Modules.Identity.Data;
using Dental.Modules.Identity.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Dental.Modules.Identity.Features.v1.Users.SearchUsers;

/// <summary>Pages through the users of the current tenant.</summary>
/// <param name="context">The identity context.</param>
public sealed class SearchUsersQueryHandler(IdentityModuleDbContext context)
    : IQueryHandler<SearchUsersQuery, PagedResponse<UserDto>>
{
    /// <inheritdoc />
    public async ValueTask<PagedResponse<UserDto>> Handle(
        SearchUsersQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        IQueryable<DentalUser> users = context.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            string term = $"%{query.SearchTerm.Trim()}%";
            users = users.Where(u =>
                EF.Functions.ILike(u.Email!, term)
                || EF.Functions.ILike(u.FirstName, term)
                || EF.Functions.ILike(u.LastName, term));
        }

        if (query.IsActive is { } isActive)
        {
            users = users.Where(u => u.IsActive == isActive);
        }

        if (!string.IsNullOrWhiteSpace(query.Role))
        {
            users = users.Where(u => context.UserRoles
                .Join(context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
                .Any(x => x.UserId == u.Id && x.Name == query.Role));
        }

        users = query.Sort switch
        {
            "email asc" => users.OrderBy(u => u.Email),
            "email desc" => users.OrderByDescending(u => u.Email),
            "lastName desc" => users.OrderByDescending(u => u.LastName),
            _ => users.OrderBy(u => u.LastName).ThenBy(u => u.FirstName),
        };

        PagedResponse<UserDto> page = await users
            .Select(u => new UserDto(
                u.Id,
                u.Email ?? string.Empty,
                u.FirstName,
                u.LastName,
                u.PhoneNumber,
                u.IsActive,
                u.EmailConfirmed,
                new List<string>(),
                u.CreatedAt))
            .ToPagedResponseAsync(query, cancellationToken)
            .ConfigureAwait(false);

        return await AttachRolesAsync(page, cancellationToken).ConfigureAwait(false);
    }

    private async Task<PagedResponse<UserDto>> AttachRolesAsync(
        PagedResponse<UserDto> page,
        CancellationToken cancellationToken)
    {
        if (page.Items.Count == 0)
        {
            return page;
        }

        Guid[] ids = [.. page.Items.Select(u => u.Id)];

        Dictionary<Guid, List<string>> rolesByUser = await context.UserRoles
            .AsNoTracking()
            .Where(ur => ids.Contains(ur.UserId))
            .Join(context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
            .GroupBy(x => x.UserId)
            .ToDictionaryAsync(
                g => g.Key,
                g => g.Select(x => x.Name ?? string.Empty).ToList(),
                cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<UserDto> items =
        [
            .. page.Items.Select(u => u with
            {
                Roles = rolesByUser.TryGetValue(u.Id, out List<string>? roles) ? roles : [],
            }),
        ];

        return page with { Items = items };
    }
}
