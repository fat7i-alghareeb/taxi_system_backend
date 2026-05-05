using System.ComponentModel.DataAnnotations;
using Taxi.Contracts.Common;

namespace Taxi.Contracts.Requests.Vehicles;

public class UpdateVehicleRequest
{
    public string Color { get; set; } = string.Empty;

    [Required(ErrorMessage = LocalizationKeys.Vehicle.LicensePlateRequired)]
    public string LicensePlate { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
