using System.ComponentModel.DataAnnotations;
using Taxi.Contracts.Common;

namespace Taxi.Contracts.Requests.Trips;

public class UpdatePassengerNoteRequest
{
    [MaxLength(500, ErrorMessage = LocalizationKeys.Trip.PassengerNoteTooLong)]
    public string? PassengerNote { get; set; }
}
