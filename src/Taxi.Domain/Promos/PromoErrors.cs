using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Domain.Promos;

public static class PromoErrors
{
    public static readonly Error CodeRequired = Error.Validation(
        code: LocalizationKeys.Promo.CodeRequired,
        description: "Promo code is required.");

    public static readonly Error NotFound = Error.NotFound(
        code: LocalizationKeys.Promo.NotFound,
        description: "Promo code not found.");
}
