using Taxi.Domain.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Domain.Vehicles;

public sealed class VehicleType : AuditableEntity
{
    private VehicleType() { } // EF Core

    private VehicleType(
        Guid id,
        string code,
        LocalizedText name,
        int passengerCapacity,
        decimal ratePerKm,
        decimal ratePerMin,
        decimal minimumFare,
        string currencyCode,
        int sortOrder)
        : base(id)
    {
        Code = code;
        Name = name;
        PassengerCapacity = passengerCapacity;
        RatePerKm = ratePerKm;
        RatePerMin = ratePerMin;
        MinimumFare = minimumFare;
        CurrencyCode = currencyCode;
        SortOrder = sortOrder;
        IsActive = true;
    }

    public string Code { get; private set; } = default!;
    public LocalizedText Name { get; private set; } = default!;
    public int PassengerCapacity { get; private set; }
    public decimal RatePerKm { get; private set; }
    public decimal RatePerMin { get; private set; }
    public decimal MinimumFare { get; private set; }
    public string CurrencyCode { get; private set; } = "EUR";
    public bool IsActive { get; private set; }
    public int SortOrder { get; private set; }

    public static Result<VehicleType> Create(
        Guid id,
        string code,
        string nameEn,
        string nameAr,
        string nameNl,
        int passengerCapacity,
        decimal ratePerKm,
        decimal ratePerMin,
        decimal minimumFare,
        string currencyCode = "EUR",
        int sortOrder = 0)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return VehicleErrors.CodeRequired;
        }

        if (string.IsNullOrWhiteSpace(nameEn))
        {
            return VehicleErrors.NameEnRequired;
        }

        if (string.IsNullOrWhiteSpace(nameAr))
        {
            return VehicleErrors.NameArRequired;
        }

        if (string.IsNullOrWhiteSpace(nameNl))
        {
            return VehicleErrors.NameNlRequired;
        }

        var name = new LocalizedText(nameEn, nameAr, nameNl);
        return new VehicleType(id, code, name, passengerCapacity, ratePerKm, ratePerMin, minimumFare, currencyCode, sortOrder);
    }

    public void UpdatePricing(decimal ratePerKm, decimal ratePerMin, decimal minimumFare)
    {
        RatePerKm = ratePerKm;
        RatePerMin = ratePerMin;
        MinimumFare = minimumFare;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
