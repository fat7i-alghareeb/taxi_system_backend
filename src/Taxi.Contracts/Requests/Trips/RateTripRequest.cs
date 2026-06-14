using System.ComponentModel.DataAnnotations;
using Taxi.Contracts.Common;

namespace Taxi.Contracts.Requests.Trips;

public class RateTripRequest
{
    [Range(1, 5, ErrorMessage = LocalizationKeys.Trip.InvalidRating)]
    public int Stars { get; set; }

    [MaxLength(500, ErrorMessage = LocalizationKeys.Trip.PassengerNoteTooLong)]
    public string? Comment { get; set; }
}
