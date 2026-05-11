using Taxi.Contracts.Common;
using Taxi.Domain.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Domain.Trips;

public sealed class PricingQuote : Entity
{
    private readonly List<Coordinate> _stops = [];

    private PricingQuote() { }

    private PricingQuote(
        Guid id,
        Guid passengerId,
        Guid vehicleTypeId,
        decimal totalDistanceKm,
        decimal totalDurationMin,
        decimal finalFare,
        string currencyCode,
        DateTime validUntil,
        IEnumerable<Coordinate> stops)
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
        _stops.AddRange(stops);
    }

    public static Result<PricingQuote> Create(
        Guid id,
        Guid passengerId,
        Guid vehicleTypeId,
        decimal totalDistanceKm,
        decimal totalDurationMin,
        decimal finalFare,
        string currencyCode,
        DateTime validUntil,
        IEnumerable<Coordinate> stops)
    {
        if (passengerId == Guid.Empty)
        {
            return Error.Validation(LocalizationKeys.Trip.PassengerNotFound, "Passenger ID is required.");
        }

        if (vehicleTypeId == Guid.Empty)
        {
            return Error.Validation(LocalizationKeys.Trip.VehicleTypeNotFound, "Vehicle type ID is required.");
        }

        if (totalDistanceKm < 0)
        {
            return Error.Validation(LocalizationKeys.Validation.InvalidFormat, "Total distance cannot be negative.");
        }

        if (totalDurationMin < 0)
        {
            return Error.Validation(LocalizationKeys.Validation.InvalidFormat, "Total duration cannot be negative.");
        }

        if (finalFare < 0)
        {
            return Error.Validation(LocalizationKeys.Payment.InvalidAmount, "Final fare cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            return Error.Validation(LocalizationKeys.Validation.RequiredField, "Currency code is required.");
        }

        return new PricingQuote(
            id,
            passengerId,
            vehicleTypeId,
            totalDistanceKm,
            totalDurationMin,
            finalFare,
            currencyCode,
            validUntil,
            stops);
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
    public IReadOnlyCollection<Coordinate> Stops => _stops.AsReadOnly();

    public bool IsExpired() => DateTime.UtcNow > ValidUntil;
    public void MarkAsUsed() => Used = true;
}

