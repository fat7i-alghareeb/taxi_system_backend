using System.ComponentModel.DataAnnotations;
using Taxi.Contracts.Common;

namespace Taxi.Contracts.Requests.Users;

public class UpdateProfileRequest
{
    [Required(ErrorMessage = LocalizationKeys.User.ProfileNameRequired)]
    public string Name { get; set; } = default!;
}
