using System.ComponentModel.DataAnnotations;
using Taxi.Contracts.Common;

namespace Taxi.Contracts.Requests.Drivers;

public class UpdateDriverRequest
{
    [Required(ErrorMessage = LocalizationKeys.Driver.LicenseRequired)]
    public string LicenseNumber { get; set; } = string.Empty;
}
