using System.ComponentModel.DataAnnotations;

using Taxi.Contracts.Common;

namespace Taxi.Contracts.Requests.Maps;

public class SearchPlacesRequest
{
    [Required(ErrorMessage = LocalizationKeys.Maps.QueryRequired)]
    public string Query { get; set; } = default!;

    [Required(ErrorMessage = LocalizationKeys.Maps.LocationRequired)]
    public decimal? Latitude { get; set; }

    [Required(ErrorMessage = LocalizationKeys.Maps.LocationRequired)]
    public decimal? Longitude { get; set; }
}
