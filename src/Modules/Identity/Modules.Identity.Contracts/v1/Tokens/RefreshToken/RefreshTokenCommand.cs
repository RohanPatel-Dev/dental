using Dental.Modules.Identity.Contracts.Dtos;
using Mediator;

namespace Dental.Modules.Identity.Contracts.v1.Tokens.RefreshToken;

/// <summary>Exchanges a refresh token for a new pair, rotating the refresh token.</summary>
/// <param name="RefreshToken">The opaque refresh token previously issued.</param>
public sealed record RefreshTokenCommand(string RefreshToken) : ICommand<TokenDto>;
