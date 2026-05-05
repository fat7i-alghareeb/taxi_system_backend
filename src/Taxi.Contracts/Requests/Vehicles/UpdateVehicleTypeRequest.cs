using System.ComponentModel.DataAnnotations;
using Taxi.Contracts.Common;

namespace Taxi.Contracts.Requests.Vehicles;

public class UpdateVehicleTypeRequest
{
    [Range(0, 1000, ErrorMessage = LocalizationKeys.Validation.InvalidFormat)]
    public decimal RatePerKm { get; set; }

    [Range(0, 1000, ErrorMessage = LocalizationKeys.Validation.InvalidFormat)]
    public decimal RatePerMin { get; set; }

    [Range(0, 1000, ErrorMessage = LocalizationKeys.Validation.InvalidFormat)]
    public decimal MinFare { get; set; }

    public bool IsActive { get; set; } = true;
}
