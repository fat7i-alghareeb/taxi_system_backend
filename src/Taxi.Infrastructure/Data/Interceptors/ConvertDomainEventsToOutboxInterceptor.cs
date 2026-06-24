using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

using Taxi.Domain.Common;
using Taxi.Infrastructure.Outbox;

namespace Taxi.Infrastructure.Data.Interceptors;

/// <summary>
/// Transactional-outbox enqueue step. Just before EF persists, this interceptor
/// collects the domain events raised by tracked aggregates, serializes each into
/// an <see cref="OutboxMessage"/> row, and adds those rows to the SAME change set.
/// Writing the messages in the same transaction as the state change that produced
/// them is what makes the outbox reliable: an event can never be lost, nor
/// dispatched for a change that was rolled back. The events are then cleared from
/// the entities so they are not enqueued twice. Actual delivery happens later, in
/// <see cref="OutboxDispatcherService"/>.
///
/// This replaces the previous override inside <c>AppDbContext.SaveChangesAsync</c>
/// to match the codebase's SaveChanges-interceptor convention (see
/// <see cref="AuditableEntityInterceptor"/>, <see cref="AuditLogInterceptor"/>),
/// and additionally covers the synchronous save path that the old override missed.
/// </summary>
public sealed class ConvertDomainEventsToOutboxInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        AddDomainEventsToOutbox(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        AddDomainEventsToOutbox(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void AddDomainEventsToOutbox(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var domainEntities = context.ChangeTracker.Entries()
            .Where(e => e.Entity is Entity baseEntity && baseEntity.DomainEvents.Count != 0)
            .Select(e => (Entity)e.Entity)
            .ToList();

        if (domainEntities.Count == 0)
        {
            return;
        }

        var messages = domainEntities
            .SelectMany(e => e.DomainEvents)
            .Select(domainEvent =>
            {
                var eventType = domainEvent.GetType();
                var typeName = eventType.AssemblyQualifiedName
                    ?? throw new InvalidOperationException("Domain event type name is unavailable.");
                return new OutboxMessage(
                    domainEvent.EventId,
                    domainEvent.OccurredAtUtc,
                    typeName,
                    JsonSerializer.Serialize(domainEvent, eventType));
            })
            .ToList();

        context.Set<OutboxMessage>().AddRange(messages);

        foreach (var entity in domainEntities)
        {
            entity.ClearDomainEvents();
        }
    }
}
