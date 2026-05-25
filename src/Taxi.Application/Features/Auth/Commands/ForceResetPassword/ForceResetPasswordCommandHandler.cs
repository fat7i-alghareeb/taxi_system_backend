using MediatR;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Identity.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Auth.Commands.ForceResetPassword;

public class ForceResetPasswordCommandHandler(
    IIdentityService identityService,
    ITokenProvider tokenProvider,
    IUser currentUser) : IRequestHandler<ForceResetPasswordCommand, Result<TokenResponse>>
{
    private readonly IIdentityService _identityService = identityService;
    private readonly ITokenProvider _tokenProvider = tokenProvider;

    public async Task<Result<TokenResponse>> Handle(ForceResetPasswordCommand request, CancellationToken ct)
    {
        var userId = currentUser.Id;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Error.Unauthorized("Auth.Unauthorized", "User is not authenticated.");
        }

        // 1. Reset password in identity database
        var resetResult = await _identityService.ResetPasswordAsync(userId, request.NewPassword);
        if (resetResult.IsFailure)
        {
            return resetResult.Error;
        }

        // 2. Clear password reset required flag on user entity
        var clearResult = await _identityService.ClearPasswordResetFlagAsync(userId);
        if (clearResult.IsFailure)
        {
            return clearResult.Error;
        }

        // 3. Generate a fresh JWT that does NOT contain requires_password_reset claim anymore
        var userDtoResult = await _identityService.GetUserByIdAsync(userId);
        if (userDtoResult.IsFailure)
        {
            return userDtoResult.Error;
        }

        var tokenResult = await _tokenProvider.GenerateJwtTokenAsync(userDtoResult.Value, ct);
        if (tokenResult.IsFailure)
        {
            return tokenResult.Error;
        }

        return tokenResult.Value;
    }
}
