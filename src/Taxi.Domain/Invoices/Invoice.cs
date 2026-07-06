using Taxi.Domain.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Invoices.Events;
using Taxi.Domain.Payments;

namespace Taxi.Domain.Invoices;

public sealed class Invoice : AuditableEntity
{
    private Invoice() { }

    private Invoice(
        Guid id,
        Guid tripId,
        Guid passengerId,
        string invoiceNumber,
        DateTimeOffset issuedAtUtc,
        string currencyCode,
        decimal netAmount,
        decimal taxRate,
        decimal taxAmount,
        decimal grossAmount,
        decimal fareAmount,
        decimal waitingFeeAmount,
        decimal discountAmount,
        decimal totalPaidAmount,
        decimal refundedAmount,
        decimal remainingAmount,
        PaymentMethod paymentMethod,
        string? paymentReference,
        DateTimeOffset? paidAtUtc,
        string issuerName,
        string issuerAddress,
        string? issuerVatNumber,
        string tripReferenceCode,
        DateTimeOffset? tripCompletedAtUtc,
        decimal distanceKm,
        decimal durationMin,
        string vehicleTypeName,
        string? passengerName,
        string? passengerPhone,
        string? passengerEmail,
        string? passengerAddress,
        string? stripePaymentMethodType,
        string stopsJson)
        : base(id)
    {
        TripId = tripId;
        PassengerId = passengerId;
        InvoiceNumber = invoiceNumber;
        IssuedAtUtc = issuedAtUtc;
        CurrencyCode = currencyCode;
        NetAmount = netAmount;
        TaxRate = taxRate;
        TaxAmount = taxAmount;
        GrossAmount = grossAmount;
        FareAmount = fareAmount;
        WaitingFeeAmount = waitingFeeAmount;
        DiscountAmount = discountAmount;
        TotalPaidAmount = totalPaidAmount;
        RefundedAmount = refundedAmount;
        RemainingAmount = remainingAmount;
        PaymentMethod = paymentMethod;
        PaymentReference = paymentReference;
        PaidAtUtc = paidAtUtc;
        IssuerName = issuerName;
        IssuerAddress = issuerAddress;
        IssuerVatNumber = issuerVatNumber;
        TripReferenceCode = tripReferenceCode;
        TripCompletedAtUtc = tripCompletedAtUtc;
        DistanceKm = distanceKm;
        DurationMin = durationMin;
        VehicleTypeName = vehicleTypeName;
        PassengerName = passengerName;
        PassengerPhone = passengerPhone;
        PassengerEmail = passengerEmail;
        PassengerAddress = passengerAddress;
        StripePaymentMethodType = stripePaymentMethodType;
        StopsJson = stopsJson;
    }

    public Guid TripId { get; private set; }
    public Guid PassengerId { get; private set; }
    public string InvoiceNumber { get; private set; } = default!;
    public DateTimeOffset IssuedAtUtc { get; private set; }
    public string CurrencyCode { get; private set; } = default!;
    public decimal NetAmount { get; private set; }
    public decimal TaxRate { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal GrossAmount { get; private set; }

    /// <summary>Main trip fare portion of <see cref="GrossAmount"/>, excluding waiting fees.</summary>
    public decimal FareAmount { get; private set; }

    /// <summary>Portion of <see cref="GrossAmount"/> that is the accrued waiting fee
    /// (per-minute charge beyond the free grace window). Itemised on the invoice.</summary>
    public decimal WaitingFeeAmount { get; private set; }

    /// <summary>Discount snapshot applied to the trip fare when the quote carried one.</summary>
    public decimal DiscountAmount { get; private set; }

    /// <summary>Total captured/settled payments for this invoice, before refunds.</summary>
    public decimal TotalPaidAmount { get; private set; }

    /// <summary>Total successfully refunded against payments for this trip.</summary>
    public decimal RefundedAmount { get; private set; }

    /// <summary>Amount still owed because it has not been captured yet.</summary>
    public decimal RemainingAmount { get; private set; }

    public PaymentMethod PaymentMethod { get; private set; }
    public string? PaymentReference { get; private set; }
    public DateTimeOffset? PaidAtUtc { get; private set; }
    public string IssuerName { get; private set; } = default!;
    public string IssuerAddress { get; private set; } = default!;
    public string? IssuerVatNumber { get; private set; }
    public string TripReferenceCode { get; private set; } = default!;
    public DateTimeOffset? TripCompletedAtUtc { get; private set; }
    public decimal DistanceKm { get; private set; }
    public decimal DurationMin { get; private set; }
    public string VehicleTypeName { get; private set; } = default!;
    public string? PassengerName { get; private set; }

    /// <summary>Passenger phone snapshot, printed in the "billed to" block.</summary>
    public string? PassengerPhone { get; private set; }

    /// <summary>Passenger email snapshot, printed in the "billed to" block when present.</summary>
    public string? PassengerEmail { get; private set; }

    /// <summary>Passenger home address label snapshot, printed in the "billed to" block when present.</summary>
    public string? PassengerAddress { get; private set; }

    /// <summary>
    /// Exact Stripe method used (e.g. "ideal", "klarna", "card") snapshot, so the
    /// invoice can print "Betaald via: iDEAL". Null for cash / non-Stripe payments.
    /// </summary>
    public string? StripePaymentMethodType { get; private set; }

    public string StopsJson { get; private set; } = default!;

    public static Result<Invoice> Issue(
        Guid id,
        Guid tripId,
        Guid passengerId,
        string invoiceNumber,
        DateTimeOffset issuedAtUtc,
        string currencyCode,
        decimal grossAmount,
        PaymentMethod paymentMethod,
        string? paymentReference,
        DateTimeOffset? paidAtUtc,
        string issuerName,
        string issuerAddress,
        string? issuerVatNumber,
        string tripReferenceCode,
        DateTimeOffset? tripCompletedAtUtc,
        decimal distanceKm,
        decimal durationMin,
        string vehicleTypeName,
        string? passengerName,
        string stopsJson,
        string? passengerPhone = null,
        string? passengerEmail = null,
        string? passengerAddress = null,
        string? stripePaymentMethodType = null,
        decimal taxRate = 0m,
        decimal waitingFeeAmount = 0m,
        decimal? fareAmount = null,
        decimal discountAmount = 0m,
        decimal? totalPaidAmount = null,
        decimal refundedAmount = 0m,
        decimal? remainingAmount = null)
    {
        if (tripId == Guid.Empty)
        {
            return InvoiceErrors.TripIdRequired;
        }

        if (passengerId == Guid.Empty)
        {
            return InvoiceErrors.PassengerIdRequired;
        }

        if (string.IsNullOrWhiteSpace(invoiceNumber))
        {
            return InvoiceErrors.NumberRequired;
        }

        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            return InvoiceErrors.CurrencyRequired;
        }

        if (grossAmount < 0)
        {
            return InvoiceErrors.InvalidAmount;
        }

        var normalizedWaitingFeeAmount = RoundMoney(waitingFeeAmount);
        var normalizedFareAmount = RoundMoney(fareAmount ?? Math.Max(0m, grossAmount - normalizedWaitingFeeAmount));
        var normalizedDiscountAmount = RoundMoney(discountAmount);
        var normalizedTotalPaidAmount = RoundMoney(totalPaidAmount ?? (paidAtUtc is null ? 0m : grossAmount));
        var normalizedRefundedAmount = RoundMoney(refundedAmount);
        var normalizedRemainingAmount = RoundMoney(remainingAmount ?? Math.Max(0m, grossAmount - normalizedTotalPaidAmount));

        if (normalizedFareAmount < 0 ||
            normalizedWaitingFeeAmount < 0 ||
            normalizedDiscountAmount < 0 ||
            normalizedTotalPaidAmount < 0 ||
            normalizedRefundedAmount < 0 ||
            normalizedRemainingAmount < 0)
        {
            return InvoiceErrors.InvalidAmount;
        }

        if (taxRate < 0 || taxRate >= 1)
        {
            return InvoiceErrors.InvalidTaxRate;
        }

        var normalizedGrossAmount = RoundMoney(grossAmount);

        // Treat grossAmount as tax-inclusive: net + tax = gross.
        var taxAmount = taxRate == 0m
            ? 0m
            : RoundMoney(normalizedGrossAmount * taxRate / (1 + taxRate));
        var netAmount = RoundMoney(normalizedGrossAmount - taxAmount);

        var invoice = new Invoice(
            id,
            tripId,
            passengerId,
            invoiceNumber,
            issuedAtUtc,
            currencyCode,
            netAmount,
            taxRate,
            taxAmount,
            normalizedGrossAmount,
            normalizedFareAmount,
            normalizedWaitingFeeAmount,
            normalizedDiscountAmount,
            normalizedTotalPaidAmount,
            normalizedRefundedAmount,
            normalizedRemainingAmount,
            paymentMethod,
            paymentReference,
            paidAtUtc,
            issuerName,
            issuerAddress,
            issuerVatNumber,
            tripReferenceCode,
            tripCompletedAtUtc,
            distanceKm,
            durationMin,
            vehicleTypeName,
            passengerName,
            passengerPhone,
            passengerEmail,
            passengerAddress,
            stripePaymentMethodType,
            stopsJson);

        invoice.AddDomainEvent(new InvoiceIssued
        {
            InvoiceId = invoice.Id,
            TripId = tripId,
            PassengerId = passengerId,
            InvoiceNumber = invoiceNumber,
            GrossAmount = normalizedGrossAmount,
            CurrencyCode = currencyCode,
        });

        return invoice;
    }

    private static decimal RoundMoney(decimal amount)
        => decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
}
