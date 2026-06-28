using MediatR;
using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Domain.CustomerIncidents;
using Taxi.Domain.Trips.Events;

namespace Taxi.Application.Features.CustomerIncidents.EventHandlers;

/// <summary>Records an informational incident whenever a passenger is refunded.</summary>
public sealed class TripRefundedIncidentHandler(IAppDbContext context, ICustomerIncidentRecorder recorder)
    : INotificationHandler<TripRefunded>
{
    public async Task Handle(TripRefunded notification, CancellationToken ct)
    {
        var currency = await context.Trips
            .AsNoTracking()
            .Where(t => t.Id == notification.TripId)
            .Join(
                context.PricingQuotes.AsNoTracking(),
                trip => trip.QuoteId,
                quote => quote.Id,
                (trip, quote) => quote.CurrencyCode)
            .FirstOrDefaultAsync(ct);

        await recorder.RecordAsync(
            CustomerIncidentType.Refunded,
            CustomerIncidentSeverity.Info,
            notification.PassengerId,
            title: "Refund issued",
            sourceEventId: notification.EventId,
            tripId: notification.TripId,
            amount: notification.Amount,
            currencyCode: string.IsNullOrWhiteSpace(currency) ? "EUR" : currency,
            ct: ct);
    }
}
