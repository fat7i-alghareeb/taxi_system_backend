using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Vehicles;

namespace Taxi.Application.Common.Services;

public class PricingService : IPricingService
{
    public decimal CalculateFare(VehicleType vehicleType, double distanceKm, double durationMin)
    {
        var distanceFare = (decimal)distanceKm * vehicleType.RatePerKm;
        var durationFare = (decimal)durationMin * vehicleType.RatePerMin;

        var totalFare = distanceFare + durationFare;
        var finalFare = Math.Max(totalFare, vehicleType.MinimumFare);

        return Math.Round(finalFare, 2);
    }
}

