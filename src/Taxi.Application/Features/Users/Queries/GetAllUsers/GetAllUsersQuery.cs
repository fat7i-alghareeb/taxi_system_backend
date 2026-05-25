using MediatR;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Users.Queries.GetAllUsers;

public record UserDto(
    Guid Id,
    string Name,
    string Phone,
    string? Email,
    string Role,
    bool IsActive,
    DateTimeOffset CreatedAtUtc);

public record GetAllUsersQuery(int PageNumber = 1, int PageSize = 15) : IRequest<Result<List<UserDto>>>;
