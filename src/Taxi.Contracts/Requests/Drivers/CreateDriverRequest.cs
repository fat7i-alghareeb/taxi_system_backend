using System.ComponentModel.DataAnnotations;
using Taxi.Contracts.Common;

namespace Taxi.Contracts.Requests.Drivers;

public class CreateDriverRequest
{
    [Required(ErrorMessage = LocalizationKeys.Validation.RequiredField)]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = LocalizationKeys.Validation.EnglishNameRequired)]
    public string NameEn { get; set; } = string.Empty;

    [Required(ErrorMessage = LocalizationKeys.Validation.ArabicNameRequired)]
    public string NameAr { get; set; } = string.Empty;

    [Required(ErrorMessage = LocalizationKeys.Validation.RequiredField)]
    public string NameNl { get; set; } = string.Empty;

    [Required(ErrorMessage = LocalizationKeys.Validation.RequiredField)]
    public string NameDe { get; set; } = string.Empty;

    [Required(ErrorMessage = LocalizationKeys.Validation.RequiredField)]
    public string NamePl { get; set; } = string.Empty;

    [Required(ErrorMessage = LocalizationKeys.Validation.RequiredField)]
    public string NameUk { get; set; } = string.Empty;

    [Required(ErrorMessage = LocalizationKeys.Validation.RequiredField)]
    public string NameFr { get; set; } = string.Empty;

    [Required(ErrorMessage = LocalizationKeys.Validation.RequiredField)]
    public string NameEs { get; set; } = string.Empty;

    [Required(ErrorMessage = LocalizationKeys.Validation.RequiredField)]
    public string NameRo { get; set; } = string.Empty;

    [Required(ErrorMessage = LocalizationKeys.Driver.LicenseRequired)]
    public string LicenseNumber { get; set; } = string.Empty;
}

