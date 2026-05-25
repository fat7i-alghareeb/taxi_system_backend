using MediatR;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Users.Commands.UpdateFcmToken;

public record UpdateFcmTokenCommand(string FcmToken) : IRequest<Result<Success>>;
