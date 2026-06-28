using MediatR;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Users.Queries.GetAllUsers;

public record GetAllUsersQuery(
    int PageNumber = 1,
    int PageSize = 15,
    string? Search = null,
    string? Role = null) : IRequest<Result<List<UserDto>>>;

public record UserDto(
    Guid Id,
    string Name,
    string Phone,
    string? Email,
    string Role,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    string? ProfilePhotoUrl,
    string? HomeAddressLabel,
    decimal? HomeAddressLatitude,
    decimal? HomeAddressLongitude);
