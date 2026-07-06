using Taxi.Domain.Common.Results;

namespace Taxi.Application.Common.Interfaces;

/// <summary>
/// Verified-identity carried by a short-lived registration token. Google/email sign-up
/// proves ownership of an email first; a full account is only created once the user also
/// supplies the required phone number (see the register/complete flow).
/// </summary>
public sealed record RegistrationTokenPayload(string Email, string? Name, string? GoogleId, bool EmailVerified);

/// <summary>
/// Issues and validates short-lived signed registration tokens (scope=registration).
/// </summary>
public interface IRegistrationTokenService
{
    string Issue(RegistrationTokenPayload payload);

    Result<RegistrationTokenPayload> Validate(string token);
}
