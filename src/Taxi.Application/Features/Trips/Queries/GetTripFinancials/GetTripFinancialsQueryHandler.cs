using MediatR;
using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Common;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Payments;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Queries.GetTripFinancials;

public class GetTripFinancialsQueryHandler(IAppDbContext context)
    : IRequestHandler<GetTripFinancialsQuery, Result<TripFinancialsDto>>
{
    public async Task<Result<TripFinancialsDto>> Handle(GetTripFinancialsQuery request, CancellationToken ct)
    {
        var trip = await context.Trips.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TripId, ct);
        if (trip is null)
        {
            return TripErrors.NotFound;
        }

        var quote = await context.PricingQuotes.AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == trip.QuoteId, ct);

        var payments = await context.Payments.AsNoTracking()
            .Where(p => p.TripId == trip.Id)
            .ToListAsync(ct);

        var refunds = await context.PaymentRefunds.AsNoTracking()
            .Where(r => r.TripId == trip.Id)
            .ToListAsync(ct);

        var waitingSessions = await context.TripWaitingSessions.AsNoTracking()
            .Where(s => s.TripId == trip.Id)
            .ToListAsync(ct);

        var currency = quote?.CurrencyCode ?? payments.FirstOrDefault()?.Currency ?? "EUR";
        var fare = quote?.FinalFare
            ?? payments.Where(p => p.Kind == PaymentKind.Fare).Sum(p => p.Amount);

        return TripFinancialsCalculator.Compute(fare, currency, payments, refunds, waitingSessions);
    }
}
