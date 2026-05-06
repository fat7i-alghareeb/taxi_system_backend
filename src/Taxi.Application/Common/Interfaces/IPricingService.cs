using Taxi.Domain.Vehicles;

namespace Taxi.Application.Common.Interfaces;

public interface IPricingService
{
    decimal CalculateFare(VehicleType vehicleType, double distanceKm, double durationMin);
}
