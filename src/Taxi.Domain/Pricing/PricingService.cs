using Taxi.Domain.Vehicles;

namespace Taxi.Domain.Pricing;

// Pure domain service: stateless math over a VehicleType aggregate, no IO, no
// external dependencies. Lives in Domain because it encodes a business invariant
// (how a fare is computed from distance/duration/rates). No interface because
// nothing about the calculation is meaningfully substitutable at runtime — if
// pricing ever grows infrastructure dependencies (DB-driven tariffs, surge API),
// introduce an IPricingService port in Application at that point.
public static class PricingService
{
    public static decimal CalculateFare(VehicleType vehicleType, double distanceKm, double durationMin)
    {
        var distanceFare = (decimal)distanceKm * vehicleType.RatePerKm;
        var durationFare = (decimal)durationMin * vehicleType.RatePerMin;

        var totalFare = distanceFare + durationFare;

        return Math.Round(totalFare, 2);
    }
}
