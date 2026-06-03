using MediatR;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Users.Commands.DeleteCurrentUser;

public record DeleteCurrentUserCommand() : IRequest<Result<Success>>;
