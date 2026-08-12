namespace Taxi.Application.Common.Interfaces;

/// <summary>
/// Terminates persisted sessions by revoking refresh tokens.
///
/// Access tokens are self-contained and stay valid until they expire, so revocation is the
/// only server-side way to end a session. Every state change that should end a session —
/// suspension, deactivation, account deletion, password change, logout — must call this;
/// otherwise the holder keeps rotating refresh tokens for the full refresh lifetime
/// (30 days by default) regardless of the account's state.
/// </summary>
public interface ISessionRevoker
{
    /// <summary>Revokes every live refresh token for the user. Returns the number revoked.</summary>
    Task<int> RevokeAllForUserAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Revokes a single presented refresh token (logout on one device). Returns true when a
    /// live row was revoked.
    /// </summary>
    Task<bool> RevokeTokenAsync(string userId, string refreshToken, CancellationToken ct = default);
}
