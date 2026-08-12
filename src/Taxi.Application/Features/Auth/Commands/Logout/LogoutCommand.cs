using MediatR;

using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Auth.Commands.Logout;

/// <summary>
/// Ends the caller's session server-side.
/// </summary>
/// <param name="RefreshToken">
/// The refresh token to revoke (single device). When omitted, every live session for the
/// caller is revoked — used for "sign out everywhere" and after a suspected compromise.
/// </param>
public sealed record LogoutCommand(string? RefreshToken, bool AllDevices = false) : IRequest<Result<Success>>;
