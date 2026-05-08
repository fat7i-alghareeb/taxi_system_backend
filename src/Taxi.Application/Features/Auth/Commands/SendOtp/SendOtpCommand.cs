using MediatR;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Auth.Commands.SendOtp;

public sealed record SendOtpCommand(string Phone) : IRequest<Result<string>>;

