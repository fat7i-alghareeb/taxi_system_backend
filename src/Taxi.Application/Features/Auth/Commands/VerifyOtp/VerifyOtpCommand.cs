using MediatR;
using Taxi.Application.Features.Auth.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Auth.Commands.VerifyOtp;

public sealed record VerifyOtpCommand(
    string Phone,
    string SessionToken,
    string Code) : IRequest<Result<AuthResponse>>;

