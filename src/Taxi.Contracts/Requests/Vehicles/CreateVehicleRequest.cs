using System.ComponentModel.DataAnnotations;
using Taxi.Contracts.Common;

namespace Taxi.Contracts.Requests.Vehicles;

public class CreateVehicleRequest
{
    [Required(ErrorMessage = LocalizationKeys.Vehicle.TypeIdRequired)]
    public Guid VehicleTypeId { get; set; }

    [Required(ErrorMessage = LocalizationKeys.Validation.RequiredField)]
    public Guid DriverId { get; set; }

    [Required(ErrorMessage = LocalizationKeys.Vehicle.MakeRequired)]
    public string Make { get; set; } = string.Empty;

    [Required(ErrorMessage = LocalizationKeys.Vehicle.ModelRequired)]
    public string Model { get; set; } = string.Empty;

    [Required(ErrorMessage = LocalizationKeys.Validation.RequiredField)]
    public string Year { get; set; } = string.Empty;

    public string Color { get; set; } = string.Empty;

    [Required(ErrorMessage = LocalizationKeys.Vehicle.LicensePlateRequired)]
    public string LicensePlate { get; set; } = string.Empty;
}
