using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Users;

namespace Taxi.Application.Features.Users.Queries.GetAllUsers;

public class GetAllUsersQueryHandler(IAppDbContext context)
    : IRequestHandler<GetAllUsersQuery, Result<List<UserDto>>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<List<UserDto>>> Handle(GetAllUsersQuery request, CancellationToken ct)
    {
        var query = _context.DomainUsers
            .AsNoTracking()
            .Where(u => u.DeletedAtUtc == null);

        if (!string.IsNullOrWhiteSpace(request.Role)
            && Enum.TryParse<UserRole>(request.Role, true, out var roleFilter))
        {
            query = query.Where(u => u.Role == roleFilter);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(u =>
                u.Name.ToLower().Contains(search)
                || u.Phone.ToLower().Contains(search)
                || (u.Email != null && u.Email.ToLower().Contains(search)));
        }

        var users = await query
            .OrderByDescending(u => u.CreatedAtUtc)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(u => new UserDto(
                u.Id,
                u.Name,
                u.Phone,
                u.Email,
                u.Role.ToString(),
                u.IsActive,
                u.CreatedAtUtc,
                u.ProfilePhotoUrl,
                u.HomeAddress != null ? u.HomeAddress.Label : null,
                u.HomeAddress != null ? u.HomeAddress.Latitude : null,
                u.HomeAddress != null ? u.HomeAddress.Longitude : null))
            .ToListAsync(ct);

        return users;
    }
}
