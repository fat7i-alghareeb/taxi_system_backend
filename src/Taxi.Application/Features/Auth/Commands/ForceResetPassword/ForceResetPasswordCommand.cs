using MediatR;
using Taxi.Application.Features.Identity.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Auth.Commands.ForceResetPassword;

public record ForceResetPasswordCommand(string NewPassword) : IRequest<Result<TokenResponse>>;
