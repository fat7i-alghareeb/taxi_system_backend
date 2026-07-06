using System.ComponentModel.DataAnnotations;

namespace Taxi.Contracts.Requests.Trips;

public class UpdateTripStopItemRequest
{
    [Required]
    public decimal Latitude { get; set; }

    [Required]
    public decimal Longitude { get; set; }

    public string? Label { get; set; }
}

public class UpdateTripStopsRequest
{
    [Required]
    [MinLength(2)]
    public List<UpdateTripStopItemRequest> Stops { get; set; } = [];
}
