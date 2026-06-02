using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Common.Options;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Payments;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Queries.GetTripReceipt;

/// <summary>
/// Returns a customer-facing receipt for a completed trip. When the trip
/// has a persisted <c>Invoice</c>, its snapshot drives the amounts; otherwise
/// the receipt is composed live from the trip + quote + payment for
/// trips that completed before invoicing was enabled.
/// </summary>
public class GetTripReceiptQueryHandler(
    IAppDbContext context,
    IUser currentUser,
    IOptions<InvoiceIssuerOptions> issuerOptions)
    : IRequestHandler<GetTripReceiptQuery, Result<TripReceiptDto>>
{
    private readonly InvoiceIssuerOptions _issuer = issuerOptions.Value;

    public async Task<Result<TripReceiptDto>> Handle(GetTripReceiptQuery request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(currentUser.Id) || !Guid.TryParse(currentUser.Id, out var userId))
        {
            return Error.Unauthorized(LocalizationKeys.Auth.UserIdClaimInvalid, "Invalid user ID claim.");
        }

        var trip = await context.Trips
            .AsNoTracking()
            .Include(t => t.Stops)
            .FirstOrDefaultAsync(t => t.Id == request.TripId, ct);

        if (trip is null)
        {
            return TripErrors.NotFound;
        }

        if (!currentUser.IsAdmin && trip.PassengerId != userId)
        {
            return TripErrors.NotOwnedByPassenger;
        }

        var invoice = await context.Invoices
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.TripId == trip.Id, ct);

        var quote = await context.PricingQuotes
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == trip.QuoteId, ct);

        var payment = await context.Payments
            .AsNoTracking()
            .Where(p => p.TripId == trip.Id)
            .OrderByDescending(p => p.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

        var vehicleType = await context.VehicleTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == trip.VehicleTypeId, ct);

        var passenger = await context.DomainUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == trip.PassengerId, ct);

        var stops = trip.Stops
            .OrderBy(s => s.Sequence)
            .Select(s => new TripStopDto(
                s.Coordinate.Latitude,
                s.Coordinate.Longitude,
                s.AddressLabel,
                s.Sequence,
                s.IsCompleted,
                s.CompletedAtUtc))
            .ToList();

        decimal gross;
        decimal net;
        decimal tax;
        string currency;
        PaymentMethod method;
        string? paymentReference;
        DateTimeOffset? paidAtUtc;

        if (invoice is not null)
        {
            gross = invoice.GrossAmount;
            net = invoice.NetAmount;
            tax = invoice.TaxAmount;
            currency = invoice.CurrencyCode;
            method = invoice.PaymentMethod;
            paymentReference = invoice.PaymentReference;
            paidAtUtc = invoice.PaidAtUtc;
        }
        else
        {
            gross = payment is { Status: PaymentStatus.Completed } ? payment.Amount : (quote?.FinalFare ?? 0m);
            net = gross;
            tax = 0m;
            currency = payment?.Currency ?? quote?.CurrencyCode ?? "EUR";
            method = payment?.Method ?? PaymentMethod.Cash;
            paymentReference = payment?.TransactionReference;
            paidAtUtc = payment?.ProcessedAtUtc is { } p ? new DateTimeOffset(DateTime.SpecifyKind(p, DateTimeKind.Utc)) : null;
        }

        return new TripReceiptDto(
            TripId: trip.Id,
            ReferenceCode: trip.ReferenceCode,
            Status: trip.Status.ToString(),
            GrossAmount: gross,
            NetAmount: net,
            TaxAmount: tax,
            CurrencyCode: currency,
            PaymentMethod: method.ToString(),
            PaymentReference: paymentReference,
            PaidAtUtc: paidAtUtc,
            CompletedAtUtc: trip.CompletedAtUtc,
            DistanceKm: quote?.TotalDistanceKm ?? 0m,
            DurationMin: quote?.TotalDurationMin ?? 0m,
            VehicleTypeName: vehicleType?.Name.En ?? "Unknown",
            PassengerName: passenger?.Name,
            IssuerName: _issuer.Name,
            InvoiceAvailable: invoice is not null,
            InvoiceNumber: invoice?.InvoiceNumber,
            InvoiceIssuedAtUtc: invoice?.IssuedAtUtc,
            Stops: stops);
    }
}
