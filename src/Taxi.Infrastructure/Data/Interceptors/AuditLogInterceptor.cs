using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Audit;
using Taxi.Infrastructure.Outbox;

namespace Taxi.Infrastructure.Data.Interceptors;

public class AuditLogInterceptor(IUser user) : SaveChangesInterceptor
{
    private readonly IUser _user = user;

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            await LogChangesAsync(eventData.Context, cancellationToken);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private async Task LogChangesAsync(DbContext context, CancellationToken ct)
    {
        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
            .ToList();

        if (!entries.Any())
        {
            return;
        }

        var userId = _user.Id != null ? Guid.Parse(_user.Id) : Guid.Empty;

        foreach (var entry in entries)
        {
            if (entry.Entity is AuditLog or OutboxMessage || entry.Metadata.IsOwned())
            {
                continue;
            }

            var idProperty = entry.Metadata.FindProperty("Id");
            var entityId = idProperty != null
                ? entry.Property("Id").CurrentValue?.ToString()
                : "N/A";

            var auditLogResult = AuditLog.Create(
                Guid.NewGuid(),
                userId == Guid.Empty ? (Guid?)null : userId,
                entry.State.ToString(),
                entry.Entity.GetType().Name,
                entityId ?? "Unknown",
                null,
                null);

            if (auditLogResult.IsSuccess)
            {
                context.Set<AuditLog>().Add(auditLogResult.Value);
            }
        }
    }
}

