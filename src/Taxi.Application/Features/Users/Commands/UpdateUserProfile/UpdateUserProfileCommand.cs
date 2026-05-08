using MediatR;
using Taxi.Application.Features.Auth.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Users.Commands.UpdateUserProfile;

public record UpdateUserProfileCommand(
    string? Name,
    Stream? PhotoStream,
    string? PhotoContentType) : IRequest<Result<UserDto>>;

