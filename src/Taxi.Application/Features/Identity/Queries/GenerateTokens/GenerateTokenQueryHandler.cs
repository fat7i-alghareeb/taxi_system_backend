namespace Taxi.Application.Features.Identity.Queries.GenerateTokens;

using MediatR;
using Microsoft.Extensions.Logging;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Identity.Dtos;
using Taxi.Domain.Common.Results;

public class GenerateTokenQueryHandler(
    ILogger<GenerateTokenQueryHandler> logger,
    IIdentityService identityService,
    ITokenProvider tokenProvider)
    : IRequestHandler<GenerateTokenQuery, Result<TokenResponse>>
{
    private readonly ILogger<GenerateTokenQueryHandler> logger = logger;
    private readonly IIdentityService identityService = identityService;
    private readonly ITokenProvider tokenProvider = tokenProvider;

    public async Task<Result<TokenResponse>> Handle(GenerateTokenQuery request, CancellationToken ct)
    {
        var checkPasswordResult = await this.identityService.AuthenticateByUserNameAsync(request.UserName, request.Password);

        if (checkPasswordResult.IsError)
        {
            // Tagged [Security] so failed admin authentication is alertable in Seq —
            // repeated failures for one account are the signal for credential stuffing.
            this.logger.LogWarning(
                "[Security] Admin login FAILED for userName {UserName}: {ErrorCode}",
                request.UserName,
                checkPasswordResult.TopError.Code);
            return checkPasswordResult.Errors;
        }

        var generateTokenResult = await this.tokenProvider.GenerateJwtTokenAsync(checkPasswordResult.Value, ct);

        if (generateTokenResult.IsError)
        {
            this.logger.LogError("Generate token error occurred for userName {UserName}: {ErrorDescription}", request.UserName, generateTokenResult.TopError.Description);
            return generateTokenResult.Errors;
        }

        this.logger.LogInformation(
            "[Security] Admin login SUCCEEDED for userName {UserName} (userId {UserId})",
            request.UserName,
            checkPasswordResult.Value.UserId);

        return generateTokenResult.Value;
    }
}

