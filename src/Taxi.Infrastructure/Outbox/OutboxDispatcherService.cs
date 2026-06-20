using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Taxi.Domain.Common;
using Taxi.Infrastructure.Data;

namespace Taxi.Infrastructure.Outbox;

public sealed class OutboxDispatcherService(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<OutboxDispatcherService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await DispatchBatchAsync(stoppingToken);
            await Task.Delay(PollInterval, timeProvider, stoppingToken);
        }
    }

    private async Task DispatchBatchAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var messages = await context.OutboxMessages
            .Where(x => x.ProcessedAtUtc == null && x.RetryCount < 10)
            .OrderBy(x => x.OccurredAtUtc)
            .Take(20)
            .ToListAsync(ct);

        foreach (var message in messages)
        {
            try
            {
                var eventType = Type.GetType(message.Type, throwOnError: true)!;
                var domainEvent = JsonSerializer.Deserialize(message.Content, eventType) as DomainEvent
                    ?? throw new InvalidOperationException($"Could not deserialize outbox event '{message.Type}'.");

                await mediator.Publish(domainEvent, ct);
                message.MarkProcessed(timeProvider.GetUtcNow());
            }
            catch (Exception ex)
            {
                message.MarkFailed(ex.ToString());
                logger.LogError(ex, "Failed to dispatch outbox message {MessageId}.", message.Id);
            }
        }

        if (messages.Count > 0)
        {
            await context.SaveChangesAsync(ct);
        }
    }
}
