using System.ComponentModel.DataAnnotations;
using Taxi.Contracts.Common;

namespace Taxi.Contracts.Requests.Trips;

public class RequestTripRequest
{
    [Required(ErrorMessage = LocalizationKeys.Validation.RequiredField)]
    public Guid VehicleTypeId { get; set; }

    [Required(ErrorMessage = LocalizationKeys.Validation.RequiredField)]
    public Guid QuoteId { get; set; }

    [Required(ErrorMessage = LocalizationKeys.Trip.InvalidStops)]
    [MinLength(2, ErrorMessage = LocalizationKeys.Trip.InvalidStops)]
    public List<TripStopRequest> Stops { get; set; } = [];
}

public class TripStopRequest
{
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string Label { get; set; } = string.Empty;
}
