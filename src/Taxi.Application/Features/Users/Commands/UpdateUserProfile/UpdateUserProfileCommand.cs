using MediatR;
using Taxi.Application.Features.Auth.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Users.Commands.UpdateUserProfile;

public enum HomeAddressUpdateMode
{
    Keep,
    Set,
    Clear,
}

public record UpdateUserProfileCommand(
    string? Name,
    string? Email,
    Stream? PhotoStream,
    string? PhotoContentType,
    HomeAddressUpdateMode HomeAddressOperation = HomeAddressUpdateMode.Keep,
    string? HomeAddressLabel = null,
    decimal? HomeAddressLatitude = null,
    decimal? HomeAddressLongitude = null) : IRequest<Result<UserDto>>;

