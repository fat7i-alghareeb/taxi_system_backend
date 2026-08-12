using FluentValidation;

namespace Taxi.Application.Features.Auth.Commands.Logout;

public sealed class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        // Either revoke one specific token, or explicitly ask for all devices. A request
        // carrying neither is ambiguous and would silently do nothing.
        RuleFor(c => c)
            .Must(c => c.AllDevices || !string.IsNullOrWhiteSpace(c.RefreshToken))
            .WithMessage("Provide a refresh token to revoke, or set allDevices to true.");
    }
}
