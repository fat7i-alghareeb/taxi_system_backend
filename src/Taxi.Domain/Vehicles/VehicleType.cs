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
        int sortOrder)
        : base(id)
    {
        Code = code;
        Name = name;
        PassengerCapacity = passengerCapacity;
        RatePerKm = ratePerKm;
        RatePerMin = ratePerMin;
        MinimumFare = minimumFare;
        SortOrder = sortOrder;
        IsActive = true;
    }

    public string Code { get; private set; } = default!;
    public LocalizedText Name { get; private set; } = default!;
    public int PassengerCapacity { get; private set; }
    public decimal RatePerKm { get; private set; }
    public decimal RatePerMin { get; private set; }
    public decimal MinimumFare { get; private set; }
    public bool IsActive { get; private set; }
    public int SortOrder { get; private set; }

    public static Result<VehicleType> Create(
        Guid id,
        string code,
        string nameEn,
        string nameAr,
        string nameNl,
        string nameDe,
        string namePl,
        string nameUk,
        string nameFr,
        string nameEs,
        string nameRo,
        int passengerCapacity,
        decimal ratePerKm,
        decimal ratePerMin,
        decimal minimumFare,
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

        if (string.IsNullOrWhiteSpace(nameDe))
        {
            return VehicleErrors.NameDeRequired;
        }

        if (string.IsNullOrWhiteSpace(namePl))
        {
            return VehicleErrors.NamePlRequired;
        }

        if (string.IsNullOrWhiteSpace(nameUk))
        {
            return VehicleErrors.NameUkRequired;
        }

        if (string.IsNullOrWhiteSpace(nameFr))
        {
            return VehicleErrors.NameFrRequired;
        }

        if (string.IsNullOrWhiteSpace(nameEs))
        {
            return VehicleErrors.NameEsRequired;
        }

        if (string.IsNullOrWhiteSpace(nameRo))
        {
            return VehicleErrors.NameRoRequired;
        }

        var name = new LocalizedText(nameEn, nameAr, nameNl, nameDe, namePl, nameUk, nameFr, nameEs, nameRo);
        return new VehicleType(id, code, name, passengerCapacity, ratePerKm, ratePerMin, minimumFare, sortOrder);
    }

    public Result<Success> UpdatePricing(decimal ratePerKm, decimal ratePerMin, decimal minimumFare)
    {
        RatePerKm = ratePerKm;
        RatePerMin = ratePerMin;
        MinimumFare = minimumFare;
        return Result.Success;
    }

    public Result<Success> UpdateSortOrder(int sortOrder)
    {
        SortOrder = sortOrder;
        return Result.Success;
    }

    public Result<Success> Deactivate()
    {
        IsActive = false;
        return Result.Success;
    }

    public Result<Success> Activate()
    {
        IsActive = true;
        return Result.Success;
    }
}

