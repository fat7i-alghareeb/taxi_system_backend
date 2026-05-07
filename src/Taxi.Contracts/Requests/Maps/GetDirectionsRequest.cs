using System.ComponentModel.DataAnnotations;
using Taxi.Contracts.Common;
using Taxi.Contracts.Requests.Trips;

namespace Taxi.Contracts.Requests.Maps;

public class GetDirectionsRequest
{
    [Required(ErrorMessage = LocalizationKeys.Maps.InsufficientStops)]
    [MinLength(2, ErrorMessage = LocalizationKeys.Maps.InsufficientStops)]
    public List<CoordinateRequest> Stops { get; set; } = [];
}
