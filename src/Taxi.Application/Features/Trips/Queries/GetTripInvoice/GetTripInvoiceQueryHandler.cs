using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Invoices;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Queries.GetTripInvoice;

public class GetTripInvoiceQueryHandler(
    IAppDbContext context,
    IUser currentUser,
    IInvoiceIssuanceService invoiceIssuance)
    : IRequestHandler<GetTripInvoiceQuery, Result<TripInvoiceDto>>
{
    public async Task<Result<TripInvoiceDto>> Handle(GetTripInvoiceQuery request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(currentUser.Id) || !Guid.TryParse(currentUser.Id, out var userId))
        {
            return Error.Unauthorized(LocalizationKeys.Auth.UserIdClaimInvalid, "Invalid user ID claim.");
        }

        var trip = await context.Trips
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TripId, ct);

        if (trip is null)
        {
            return TripErrors.NotFound;
        }

        if (!currentUser.IsAdmin && trip.PassengerId != userId)
        {
            return TripErrors.NotOwnedByPassenger;
        }

        // Lazy-issue: trips that completed before invoicing was live (or
        // whose eager issuance failed) get their invoice on first view.
        var issued = await invoiceIssuance.EnsureIssuedAsync(trip.Id, ct);
        if (issued.IsFailure)
        {
            return issued.Error;
        }

        var invoice = issued.Value;
        var stops = ParseStops(invoice.StopsJson);

        return new TripInvoiceDto(
            InvoiceId: invoice.Id,
            TripId: invoice.TripId,
            InvoiceNumber: invoice.InvoiceNumber,
            IssuedAtUtc: invoice.IssuedAtUtc,
            CurrencyCode: invoice.CurrencyCode,
            GrossAmount: invoice.GrossAmount,
            NetAmount: invoice.NetAmount,
            TaxRate: invoice.TaxRate,
            TaxAmount: invoice.TaxAmount,
            FareAmount: invoice.FareAmount,
            WaitingFeeAmount: invoice.WaitingFeeAmount,
            DiscountAmount: invoice.DiscountAmount,
            TotalPaidAmount: invoice.TotalPaidAmount,
            RefundedAmount: invoice.RefundedAmount,
            RemainingAmount: invoice.RemainingAmount,
            PaymentMethod: invoice.PaymentMethod.ToString(),
            PaymentReference: invoice.PaymentReference,
            PaidAtUtc: invoice.PaidAtUtc,
            IssuerName: invoice.IssuerName,
            IssuerAddress: invoice.IssuerAddress,
            IssuerVatNumber: invoice.IssuerVatNumber,
            TripReferenceCode: invoice.TripReferenceCode,
            TripCompletedAtUtc: invoice.TripCompletedAtUtc,
            DistanceKm: invoice.DistanceKm,
            DurationMin: invoice.DurationMin,
            VehicleTypeName: invoice.VehicleTypeName,
            PassengerName: invoice.PassengerName,
            PassengerEmail: invoice.PassengerEmail,
            Stops: stops);
    }

    internal static List<TripInvoiceStopDto> ParseStops(string stopsJson)
    {
        if (string.IsNullOrWhiteSpace(stopsJson))
        {
            return [];
        }

        try
        {
            var raw = JsonSerializer.Deserialize<List<InvoiceStopSnapshot>>(stopsJson);
            return raw is null
                ? []
                : raw.Select(s => new TripInvoiceStopDto(s.Sequence, s.Label, s.CompletedAtUtc)).ToList();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private sealed record InvoiceStopSnapshot(int Sequence, string? Label, DateTimeOffset? CompletedAtUtc);
}
