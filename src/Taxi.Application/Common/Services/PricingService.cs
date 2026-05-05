using Taxi.Domain.Vehicles;

namespace Taxi.Application.Common.Services;

public class PricingService
{
    public decimal CalculateFare(VehicleType vehicleType, int distanceMeters, int durationSeconds)
    {
        var distanceKm = (decimal)distanceMeters / 1000;
        var durationMin = (decimal)durationSeconds / 60;

        var distanceComponent = distanceKm * vehicleType.RatePerKm;
        var durationComponent = durationMin * vehicleType.RatePerMin;

        var rawFare = distanceComponent + durationComponent;

        return Math.Max(rawFare, vehicleType.MinimumFare);
    }
}
