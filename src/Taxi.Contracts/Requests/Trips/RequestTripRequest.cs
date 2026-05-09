using System.ComponentModel.DataAnnotations;
using Taxi.Contracts.Common;

namespace Taxi.Contracts.Requests.Trips;

public class RequestTripRequest
{
    [Required(ErrorMessage = LocalizationKeys.Validation.RequiredField)]
    public Guid QuoteId { get; set; }

    [Required(ErrorMessage = LocalizationKeys.Trip.InvalidStops)]
    [MinLength(2, ErrorMessage = LocalizationKeys.Trip.InvalidStops)]
    public List<CoordinateRequest> Stops { get; set; } = [];

    public DateTimeOffset? ScheduledAt { get; set; }
    public string? PickupStreetName { get; set; }
    public string? PickupHouseNumber { get; set; }
    public decimal PickupLatitude { get; set; }
    public decimal PickupLongitude { get; set; }
    public string? PickupAddress { get; set; }
}

