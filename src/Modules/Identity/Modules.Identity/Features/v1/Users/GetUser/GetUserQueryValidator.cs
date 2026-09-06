using Dental.Modules.Identity.Contracts.v1.Users.GetUser;
using FluentValidation;

namespace Dental.Modules.Identity.Features.v1.Users.GetUser;

/// <summary>Validates <see cref="GetUserQuery"/>.</summary>
public sealed class GetUserQueryValidator : AbstractValidator<GetUserQuery>
{
    /// <summary>Builds the rules.</summary>
    public GetUserQueryValidator() => RuleFor(q => q.UserId).NotEmpty();
}
