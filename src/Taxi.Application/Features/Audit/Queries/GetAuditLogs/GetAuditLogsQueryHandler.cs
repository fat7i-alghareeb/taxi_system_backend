using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Audit.Queries.GetAuditLogs;

public class GetAuditLogsQueryHandler(IAppDbContext context)
    : IRequestHandler<GetAuditLogsQuery, Result<List<AuditLogDto>>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<List<AuditLogDto>>> Handle(GetAuditLogsQuery request, CancellationToken ct)
    {
        var logs = await _context.AuditLogs
            .AsNoTracking()
            .OrderByDescending(l => l.CreatedAtUtc)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var list = new List<AuditLogDto>();

        foreach (var log in logs)
        {
            string? userName = null;
            if (log.UserId.HasValue)
            {
                var user = await _context.DomainUsers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == log.UserId.Value, ct);
                userName = user?.Name;
            }

            list.Add(new AuditLogDto(
                log.Id,
                log.Action,
                log.EntityName,
                log.EntityId,
                log.OldValue,
                log.NewValue,
                log.UserId?.ToString(),
                userName,
                log.CreatedAtUtc));
        }

        return list;
    }
}
