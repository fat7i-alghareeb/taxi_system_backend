using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Common.Options;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Configuration;
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

        var payments = await db.Payments
            .Where(p => p.TripId == trip.Id)
            .OrderBy(p => p.CreatedAtUtc)
            .ToListAsync(ct);

        var paymentIds = payments.Select(p => p.Id).ToList();
        var refunds = paymentIds.Count == 0
            ? await db.PaymentRefunds
                .Where(r => r.TripId == trip.Id && r.Status == PaymentRefundStatus.Succeeded)
                .ToListAsync(ct)
            : await db.PaymentRefunds
                .Where(r =>
                    r.Status == PaymentRefundStatus.Succeeded &&
                    (r.TripId == trip.Id || paymentIds.Contains(r.PaymentId)))
                .ToListAsync(ct);

        var farePayment = payments
            .Where(p => p.Kind == PaymentKind.Fare)
            .OrderByDescending(p => p.CreatedAtUtc)
            .FirstOrDefault();

        var capturedPayments = payments
            .Where(IsCaptured)
            .ToList();

        var capturedFarePayment = capturedPayments
            .Where(p => p.Kind == PaymentKind.Fare)
            .OrderByDescending(p => p.ProcessedAtUtc ?? p.CreatedAtUtc.UtcDateTime)
            .FirstOrDefault();

        var vehicleType = await db.VehicleTypes
            .FirstOrDefaultAsync(v => v.Id == trip.VehicleTypeId, ct);

        var passenger = await db.DomainUsers
            .FirstOrDefaultAsync(u => u.Id == trip.PassengerId, ct);

        var fareAmount = RoundMoney(capturedFarePayment?.Amount ?? quote?.FinalFare ?? farePayment?.Amount ?? 0m);
        var discountAmount = RoundMoney(Math.Max(0m, (quote?.OriginalFare ?? fareAmount) - (quote?.FinalFare ?? fareAmount)));
        var currency = capturedFarePayment?.Currency ?? farePayment?.Currency ?? quote?.CurrencyCode ?? "EUR";

        var displayPayment = capturedFarePayment
            ?? capturedPayments.OrderByDescending(p => p.ProcessedAtUtc ?? p.CreatedAtUtc.UtcDateTime).FirstOrDefault()
            ?? farePayment;

        var method = displayPayment?.Method ?? PaymentMethod.Cash;
        var paymentReference = method == PaymentMethod.Cash
            ? "cash"
            : displayPayment?.StripeChargeId ?? displayPayment?.TransactionReference;

        // Exact Stripe method (ideal/klarna/card) captured on the settled fare payment,
        // used to print "Betaald via: iDEAL". Null for cash / non-Stripe trips.
        var stripePaymentMethodType = capturedFarePayment?.StripePaymentMethodType;

        // Add any accrued waiting fee (per-minute charge beyond the free grace
        // window once the driver has arrived) to the invoiced amount. For card
        // trips the upfront Stripe charge did not include this surcharge; the
        // invoice records the true amount owed, and collecting the difference is
        // handled separately.
        var waitingFee = await db.TripWaitingSessions
            .Where(s => s.TripId == trip.Id && s.StoppedAtUtc != null)
            .Select(s => s.EstimatedFee ?? 0m)
            .ToListAsync(ct);
        var waitingFeeTotal = RoundMoney(waitingFee.Sum());
        var gross = RoundMoney(fareAmount + waitingFeeTotal);

        var totalPaidAmount = RoundMoney(capturedPayments.Sum(p => p.Amount));
        if (method == PaymentMethod.Cash)
        {
            // Cash trips do not always have a payment ledger row; a completed cash
            // ride is considered settled at completion, preserving existing behavior.
            totalPaidAmount = Math.Max(totalPaidAmount, gross);
        }

        var refundedAmount = RoundMoney(refunds.Sum(r => r.Amount));
        var remainingAmount = RoundMoney(Math.Max(0m, gross - totalPaidAmount));
        var paidAtUtc = ResolvePaidAtUtc(capturedPayments, method, trip.CompletedAtUtc, totalPaidAmount);

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

        // VAT/BTW rate (stored as a fraction, e.g. "0.09"). Prices are VAT-inclusive,
        // so this only decides how the gross is split into net + tax on the invoice.
        var vatConfig = await db.AppConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Key == AppConfigKeys.VatRate, ct);
        var taxRate = decimal.TryParse(vatConfig?.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var r)
            ? r
            : 0m;

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
            passengerEmail: passenger?.Email,
            passengerAddress: passenger?.HomeAddress?.Label,
            stripePaymentMethodType: stripePaymentMethodType,
            taxRate: taxRate,
            waitingFeeAmount: waitingFeeTotal,
            fareAmount: fareAmount,
            discountAmount: discountAmount,
            totalPaidAmount: totalPaidAmount,
            refundedAmount: refundedAmount,
            remainingAmount: remainingAmount);

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

    private static bool IsCaptured(Payment payment) =>
        payment.Status is PaymentStatus.Completed or PaymentStatus.Refunded;

    private static DateTimeOffset? ResolvePaidAtUtc(
        IReadOnlyCollection<Payment> capturedPayments,
        PaymentMethod method,
        DateTimeOffset? tripCompletedAtUtc,
        decimal totalPaidAmount)
    {
        if (totalPaidAmount <= 0m)
        {
            return null;
        }

        var latestProcessed = capturedPayments
            .Where(p => p.ProcessedAtUtc.HasValue)
            .OrderByDescending(p => p.ProcessedAtUtc)
            .Select(p => p.ProcessedAtUtc!.Value)
            .FirstOrDefault();

        if (latestProcessed != default)
        {
            return new DateTimeOffset(DateTime.SpecifyKind(latestProcessed, DateTimeKind.Utc));
        }

        return method == PaymentMethod.Cash ? tripCompletedAtUtc : null;
    }

    private static decimal RoundMoney(decimal amount) =>
        decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
}
