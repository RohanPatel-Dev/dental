using Dental.Modules.Identity.Contracts.Dtos;
using Mediator;

namespace Dental.Modules.Identity.Contracts.v1.Tokens.IssueToken;

/// <summary>Exchanges credentials for a token pair.</summary>
/// <param name="Email">Sign-in address.</param>
/// <param name="Password">Password.</param>
/// <param name="App">Requesting application, from the <c>X-App</c> header.</param>
public sealed record IssueTokenCommand(string Email, string Password, string App) : ICommand<TokenDto>;
