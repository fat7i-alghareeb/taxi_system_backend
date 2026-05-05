using System.ComponentModel.DataAnnotations;
using Taxi.Contracts.Common;

namespace Taxi.Contracts.Requests.Vehicles;

public class CreateVehicleTypeRequest
{
    [Required(ErrorMessage = LocalizationKeys.Vehicle.CodeRequired)]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = LocalizationKeys.Vehicle.NameEnRequired)]
    public string NameEn { get; set; } = string.Empty;

    [Required(ErrorMessage = LocalizationKeys.Vehicle.NameArRequired)]
    public string NameAr { get; set; } = string.Empty;

    [Required(ErrorMessage = LocalizationKeys.Vehicle.NameNlRequired)]
    public string NameNl { get; set; } = string.Empty;

    [Range(1, 100, ErrorMessage = LocalizationKeys.Validation.InvalidFormat)]
    public int Capacity { get; set; }

    [Range(0, 1000, ErrorMessage = LocalizationKeys.Validation.InvalidFormat)]
    public decimal RatePerKm { get; set; }

    [Range(0, 1000, ErrorMessage = LocalizationKeys.Validation.InvalidFormat)]
    public decimal RatePerMin { get; set; }

    [Range(0, 1000, ErrorMessage = LocalizationKeys.Validation.InvalidFormat)]
    public decimal MinFare { get; set; }

    [Required(ErrorMessage = LocalizationKeys.Validation.RequiredField)]
    public string Currency { get; set; } = "USD";

    public int SortOrder { get; set; }
}
