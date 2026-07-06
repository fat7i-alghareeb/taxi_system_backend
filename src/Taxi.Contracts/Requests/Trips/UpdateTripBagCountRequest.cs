using System.ComponentModel.DataAnnotations;

namespace Taxi.Contracts.Requests.Trips;

public class UpdateTripBagCountRequest
{
    [Required]
    [Range(0, 100)]
    public int BagCount { get; set; }
}
