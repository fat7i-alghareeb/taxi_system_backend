using Taxi.Domain.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Domain.PaymentMethods;

public sealed class PassengerPaymentMethod : AuditableEntity
{
    private PassengerPaymentMethod() { }

    private PassengerPaymentMethod(
        Guid id,
        Guid passengerId,
        string gatewayPaymentMethodId,
        string cardBrand,
        string lastFour,
        int expiryMonth,
        int expiryYear,
        string? cardholderName)
        : base(id)
    {
        PassengerId = passengerId;
        GatewayPaymentMethodId = gatewayPaymentMethodId;
        CardBrand = cardBrand;
        LastFour = lastFour;
        ExpiryMonth = expiryMonth;
        ExpiryYear = expiryYear;
        CardholderName = cardholderName;
        IsDefault = false;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid PassengerId { get; private set; }
    public string GatewayPaymentMethodId { get; private set; } = default!;
    public string CardBrand { get; private set; } = default!;
    public string LastFour { get; private set; } = default!;
    public int ExpiryMonth { get; private set; }
    public int ExpiryYear { get; private set; }
    public string? CardholderName { get; private set; }
    public bool IsDefault { get; private set; }
    public DateTimeOffset? DeletedAtUtc { get; private set; }

    public static Result<PassengerPaymentMethod> Create(
        Guid id,
        Guid passengerId,
        string gatewayPaymentMethodId,
        string cardBrand,
        string lastFour,
        int expiryMonth,
        int expiryYear,
        string? cardholderName)
    {
        if (passengerId == Guid.Empty)
        {
            return PassengerPaymentMethodErrors.PassengerIdRequired;
        }

        if (string.IsNullOrWhiteSpace(gatewayPaymentMethodId))
        {
            return PassengerPaymentMethodErrors.GatewayPaymentMethodIdRequired;
        }

        if (string.IsNullOrWhiteSpace(cardBrand))
        {
            return PassengerPaymentMethodErrors.CardBrandRequired;
        }

        if (string.IsNullOrWhiteSpace(lastFour) || lastFour.Length != 4 || !lastFour.All(char.IsDigit))
        {
            return PassengerPaymentMethodErrors.LastFourInvalid;
        }

        if (expiryMonth is < 1 or > 12)
        {
            return PassengerPaymentMethodErrors.ExpiryMonthInvalid;
        }

        if (expiryYear < 2000 || expiryYear > 2100)
        {
            return PassengerPaymentMethodErrors.ExpiryYearInvalid;
        }

        return new PassengerPaymentMethod(
            id,
            passengerId,
            gatewayPaymentMethodId,
            cardBrand,
            lastFour,
            expiryMonth,
            expiryYear,
            cardholderName);
    }

    public Result<Success> SetAsDefault()
    {
        IsDefault = true;
        return Result.Success;
    }

    public Result<Success> UnsetAsDefault()
    {
        IsDefault = false;
        return Result.Success;
    }

    public Result<Success> SoftDelete()
    {
        if (DeletedAtUtc.HasValue)
        {
            return Result.Success;
        }

        DeletedAtUtc = DateTimeOffset.UtcNow;
        return Result.Success;
    }
}
