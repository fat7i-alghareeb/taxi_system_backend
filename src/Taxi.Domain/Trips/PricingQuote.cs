using Taxi.Domain.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Domain.Trips;

public sealed class PricingQuote : Entity
{
    private PricingQuote() { }

    private PricingQuote(
        Guid id,
        Guid passengerId,
        Guid vehicleTypeId,
        decimal totalDistanceKm,
        decimal totalDurationMin,
        decimal finalFare,
        string currencyCode,
        DateTime validUntil)
        : base(id)
    {
        PassengerId = passengerId;
        VehicleTypeId = vehicleTypeId;
        TotalDistanceKm = totalDistanceKm;
        TotalDurationMin = totalDurationMin;
        FinalFare = finalFare;
        CurrencyCode = currencyCode;
        ValidUntil = validUntil;
        Used = false;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public static Result<PricingQuote> Create(
        Guid id,
        Guid passengerId,
        Guid vehicleTypeId,
        decimal totalDistanceKm,
        decimal totalDurationMin,
        decimal finalFare,
        string currencyCode,
        DateTime validUntil)
    {
        return new PricingQuote(
            id,
            passengerId,
            vehicleTypeId,
            totalDistanceKm,
            totalDurationMin,
            finalFare,
            currencyCode,
            validUntil);
    }

    public Guid PassengerId { get; private set; }
    public Guid VehicleTypeId { get; private set; }
    public decimal TotalDistanceKm { get; private set; }
    public decimal TotalDurationMin { get; private set; }
    public decimal FinalFare { get; private set; }
    public string CurrencyCode { get; private set; } = default!;
    public DateTime ValidUntil { get; private set; }
    public bool Used { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public bool IsExpired() => DateTime.UtcNow > ValidUntil;
    public void MarkAsUsed() => Used = true;
}
