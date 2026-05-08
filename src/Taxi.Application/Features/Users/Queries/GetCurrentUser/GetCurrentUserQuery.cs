using MediatR;
using Taxi.Application.Features.Auth.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Users.Queries.GetCurrentUser;

public record GetCurrentUserQuery : IRequest<Result<UserDto>>;

