namespace Taxi.Domain.Identity;

using Taxi.Domain.Common;
using Taxi.Domain.Common.Results;

public sealed class RefreshToken : AuditableEntity
{
    private RefreshToken()
    {
    }

    private RefreshToken(Guid id, string? token, string? userId, DateTimeOffset expiresOnUtc)
        : base(id)
    {
        this.Token = token;
        this.UserId = userId;
        this.ExpiresOnUtc = expiresOnUtc;
    }

    public string? Token { get; private set; }

    public string? UserId { get; private set; }

    public DateTimeOffset ExpiresOnUtc { get; private set; }

    /// <summary>
    /// Set when this token has been rotated (used to refresh) or explicitly revoked.
    /// Null means the token is still live. Scoped per-row so revoking one session's
    /// token never touches any other session's row for the same user.
    /// </summary>
    public DateTimeOffset? RevokedAtUtc { get; private set; }

    /// <summary>
    /// Id of the new refresh-token row issued when this one was rotated. Lets a
    /// client that retries with an already-rotated token (e.g. its first response
    /// was lost right as the device woke from idle) be handed back the same
    /// replacement pair instead of being treated as an invalid/expired session.
    /// </summary>
    public Guid? ReplacedByTokenId { get; private set; }

    public static Result<RefreshToken> Create(Guid id, string? token, string? userId, DateTimeOffset expiresOnUtc)
    {
        if (id == Guid.Empty)
        {
            return RefreshTokenErrors.IdRequired;
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            return RefreshTokenErrors.TokenRequired;
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            return RefreshTokenErrors.UserIdRequired;
        }

        if (expiresOnUtc <= DateTimeOffset.UtcNow)
        {
            return RefreshTokenErrors.ExpiryInvalid;
        }

        return new RefreshToken(id, token, userId, expiresOnUtc);
    }
}

