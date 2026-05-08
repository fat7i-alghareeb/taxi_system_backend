using MediatR;
using Taxi.Application.Features.Auth.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Users.Commands.UpdateProfile;

public record UpdateProfileCommand(string Name) : IRequest<Result<UserDto>>;

