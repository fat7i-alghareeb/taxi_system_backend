using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Common.Options;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Invoices;
using Taxi.Domain.Payments;
using Taxi.Domain.Trips;
using Taxi.Infrastructure.Data;

namespace Taxi.Infrastructure.Services.Invoices;

public sealed class InvoiceIssuanceService(
    AppDbContext db,
    IInvoiceNumberGenerator invoiceNumberGenerator,
    IOptions<InvoiceIssuerOptions> issuerOptions,
    ILogger<InvoiceIssuanceService> logger)
    : IInvoiceIssuanceService
{
    private readonly InvoiceIssuerOptions _issuer = issuerOptions.Value;

    public async Task<Result<Invoice>> EnsureIssuedAsync(Guid tripId, CancellationToken ct)
    {
        // 1) Fast-path: an invoice already exists for this trip.
        var existing = await db.Invoices
            .FirstOrDefaultAsync(i => i.TripId == tripId, ct);
        if (existing is not null)
        {
            return existing;
        }

        // 2) Load the trip + everything we need to build the snapshot.
        var trip = await db.Trips
            .Include(t => t.Stops)
            .FirstOrDefaultAsync(t => t.Id == tripId, ct);
        if (trip is null)
        {
            return TripErrors.NotFound;
        }

        if (trip.Status != TripStatus.Completed)
        {
            // Mid-flight trips have no authoritative amount to invoice yet.
            return InvoiceErrors.NotIssued;
        }

        var quote = await db.PricingQuotes
            .FirstOrDefaultAsync(q => q.Id == trip.QuoteId, ct);

        var payment = await db.Payments
            .Where(p => p.TripId == trip.Id && p.Kind == PaymentKind.Fare)
            .OrderByDescending(p => p.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

        var vehicleType = await db.VehicleTypes
            .FirstOrDefaultAsync(v => v.Id == trip.VehicleTypeId, ct);

        var passenger = await db.DomainUsers
            .FirstOrDefaultAsync(u => u.Id == trip.PassengerId, ct);

        // Stripe-charged amount wins when available; cash trips fall back
        // to the quoted final fare since there's no payment row carrying
        // the authoritative amount.
        decimal gross;
        string currency;
        PaymentMethod method;
        string? paymentReference;
        DateTimeOffset? paidAtUtc;

        // Exact Stripe method (ideal/klarna/card) captured on the completed payment,
        // used to print "Betaald via: iDEAL". Null for cash / non-Stripe trips.
        var stripePaymentMethodType = payment is { Status: PaymentStatus.Completed }
            ? payment.StripePaymentMethodType
            : null;

        if (payment is { Status: PaymentStatus.Completed, Method: PaymentMethod.CreditCard })
        {
            gross = payment.Amount;
            currency = payment.Currency;
            method = PaymentMethod.CreditCard;
            paymentReference = payment.StripeChargeId ?? payment.TransactionReference;
            paidAtUtc = payment.ProcessedAtUtc is { } p
                ? new DateTimeOffset(DateTime.SpecifyKind(p, DateTimeKind.Utc))
                : null;
        }
        else
        {
            gross = quote?.FinalFare ?? 0m;
            currency = quote?.CurrencyCode ?? "EUR";
            method = payment?.Method ?? PaymentMethod.Cash;
            paymentReference = method == PaymentMethod.Cash ? "cash" : payment?.TransactionReference;
            paidAtUtc = trip.CompletedAtUtc;
        }

        // Add any accrued waiting fee (per-minute charge beyond the free grace
        // window once the driver has arrived) to the invoiced amount. For card
        // trips the upfront Stripe charge did not include this surcharge; the
        // invoice records the true amount owed, and collecting the difference is
        // handled separately.
        var waitingFee = await db.TripWaitingSessions
            .Where(s => s.TripId == trip.Id)
            .Select(s => s.EstimatedFee ?? 0m)
            .ToListAsync(ct);
        gross += waitingFee.Sum();

        var stopsSnapshot = trip.Stops
            .OrderBy(s => s.Sequence)
            .Select(s => new
            {
                sequence = s.Sequence,
                label = s.AddressLabel,
                completedAtUtc = s.CompletedAtUtc,
            })
            .ToList();
        var stopsJson = JsonSerializer.Serialize(stopsSnapshot);

        var issuedAtUtc = DateTimeOffset.UtcNow;
        var invoiceNumber = await invoiceNumberGenerator.NextAsync(issuedAtUtc, ct);

        var result = Invoice.Issue(
            id: Guid.NewGuid(),
            tripId: trip.Id,
            passengerId: trip.PassengerId,
            invoiceNumber: invoiceNumber,
            issuedAtUtc: issuedAtUtc,
            currencyCode: currency,
            grossAmount: gross,
            paymentMethod: method,
            paymentReference: paymentReference,
            paidAtUtc: paidAtUtc,
            issuerName: _issuer.Name,
            issuerAddress: _issuer.Address,
            issuerVatNumber: _issuer.VatNumber,
            tripReferenceCode: trip.ReferenceCode,
            tripCompletedAtUtc: trip.CompletedAtUtc,
            distanceKm: quote?.TotalDistanceKm ?? 0m,
            durationMin: quote?.TotalDurationMin ?? 0m,
            vehicleTypeName: vehicleType?.Name.En ?? "Unknown",
            passengerName: passenger?.Name,
            stopsJson: stopsJson,
            passengerPhone: passenger?.Phone,
            stripePaymentMethodType: stripePaymentMethodType);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Invoice issuance failed for trip {TripId}: {Code} {Description}",
                trip.Id, result.Error.Code, result.Error.Description);
            return result.Error;
        }

        db.Invoices.Add(result.Value);
        try
        {
            await db.SaveChangesAsync(ct);
            return result.Value;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // A concurrent caller issued the invoice first. Re-fetch and return it.
            logger.LogInformation(
                "Invoice for trip {TripId} was issued concurrently; re-loading.",
                trip.Id);
            db.Entry(result.Value).State = EntityState.Detached;
            var fresh = await db.Invoices
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.TripId == trip.Id, ct);
            return fresh is not null ? fresh : InvoiceErrors.NotIssued;
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        var inner = ex.InnerException?.Message ?? string.Empty;
        return inner.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
            || inner.Contains("unique constraint", StringComparison.OrdinalIgnoreCase);
    }
}
