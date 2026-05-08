using Taxi.Domain.Common.Results;

namespace Taxi.Domain.Promos;

public static class PromoErrors
{
    public static readonly Error CodeRequired = Error.Validation(
        code: "Promo.CodeRequired",
        description: "Promo code is required.");

    public static readonly Error NotFound = Error.NotFound(
        code: "Promo.NotFound",
        description: "Promo code not found.");
}

