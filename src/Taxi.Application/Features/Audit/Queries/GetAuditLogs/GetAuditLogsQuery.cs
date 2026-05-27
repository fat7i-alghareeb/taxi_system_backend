using MediatR;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Audit.Queries.GetAuditLogs;

public record GetAuditLogsQuery(int PageNumber = 1, int PageSize = 15) : IRequest<Result<List<AuditLogDto>>>;

public record AuditLogDto(
    Guid Id,
    string Action,
    string EntityName,
    string EntityId,
    string? OldValue,
    string? NewValue,
    string? UserId,
    string? UserName,
    DateTimeOffset CreatedAtUtc);
