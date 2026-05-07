using System.ComponentModel.DataAnnotations;
using Taxi.Contracts.Common;

namespace Taxi.Contracts.Requests.Trips;

public class GetPricingQuotesRequest
{
    [Required(ErrorMessage = LocalizationKeys.Trip.InvalidStops)]
    [MinLength(2, ErrorMessage = LocalizationKeys.Trip.InvalidStops)]
    public List<CoordinateRequest> Stops { get; set; } = [];
}

public class CoordinateRequest
{
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
}
