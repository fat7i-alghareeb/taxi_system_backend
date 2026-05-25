using System.ComponentModel.DataAnnotations;

using Taxi.Contracts.Common;

namespace Taxi.Contracts.Requests.Drivers;

public class CreateDriverRequest
{
    [Required(ErrorMessage = LocalizationKeys.Validation.RequiredField)]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = LocalizationKeys.User.NameRequired)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = LocalizationKeys.Driver.LicenseRequired)]
    public string LicenseNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = LocalizationKeys.Validation.VehicleTypeIdRequired)]
    public Guid VehicleTypeId { get; set; }
}