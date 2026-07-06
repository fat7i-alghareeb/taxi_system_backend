using System.ComponentModel.DataAnnotations;

namespace Taxi.Contracts.Requests.Trips;

public class UpdateTripPassengerCountRequest
{
    [Required]
    [Range(1, 100)]
    public int PassengerCount { get; set; }
}
