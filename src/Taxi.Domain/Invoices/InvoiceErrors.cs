using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Domain.Invoices;

public static class InvoiceErrors
{
    public static readonly Error NotIssued = Error.NotFound(
        code: LocalizationKeys.Invoice.NotIssued,
        description: "An invoice has not been issued for this trip yet.");

    public static readonly Error NotFound = Error.NotFound(
        code: LocalizationKeys.Invoice.NotFound,
        description: "Invoice not found.");

    public static readonly Error AlreadyIssued = Error.Conflict(
        code: LocalizationKeys.Invoice.AlreadyIssued,
        description: "An invoice has already been issued for this trip.");

    public static readonly Error TripIdRequired = Error.Validation(
        code: LocalizationKeys.Invoice.TripIdRequired,
        description: "Trip ID is required.");

    public static readonly Error PassengerIdRequired = Error.Validation(
        code: LocalizationKeys.Invoice.PassengerIdRequired,
        description: "Passenger ID is required.");

    public static readonly Error NumberRequired = Error.Validation(
        code: LocalizationKeys.Invoice.NumberRequired,
        description: "Invoice number is required.");

    public static readonly Error CurrencyRequired = Error.Validation(
        code: LocalizationKeys.Invoice.CurrencyRequired,
        description: "Currency code is required.");

    public static readonly Error InvalidAmount = Error.Validation(
        code: LocalizationKeys.Invoice.InvalidAmount,
        description: "Invoice amount must be non-negative.");

    public static readonly Error InvalidTaxRate = Error.Validation(
        code: LocalizationKeys.Invoice.InvalidTaxRate,
        description: "Tax rate must be between 0 and 1 (exclusive).");
}
