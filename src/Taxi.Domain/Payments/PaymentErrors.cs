using Taxi.Domain.Common.Results;

namespace Taxi.Domain.Payments;

public static class PaymentErrors
{
    public static readonly Error InvalidAmount = Error.Validation(
        code: "Payment.InvalidAmount",
        description: "Amount must be greater than zero.");

    public static readonly Error NotFound = Error.NotFound(
        code: "Payment.NotFound",
        description: "Payment not found.");
}

