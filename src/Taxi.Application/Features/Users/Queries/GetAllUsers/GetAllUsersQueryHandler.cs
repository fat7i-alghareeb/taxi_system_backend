using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Users.Queries.GetAllUsers;

public class GetAllUsersQueryHandler(IAppDbContext context)
    : IRequestHandler<GetAllUsersQuery, Result<List<UserDto>>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<List<UserDto>>> Handle(GetAllUsersQuery request, CancellationToken ct)
    {
        var users = await _context.DomainUsers
            .AsNoTracking()
            .Where(u => u.DeletedAtUtc == null)
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
                u.CreatedAtUtc))
            .ToListAsync(ct);

        return users;
    }
}
