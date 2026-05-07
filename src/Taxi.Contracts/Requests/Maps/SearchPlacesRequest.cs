using System.ComponentModel.DataAnnotations;
using Taxi.Contracts.Common;

namespace Taxi.Contracts.Requests.Maps;

public class SearchPlacesRequest
{
    [Required(ErrorMessage = LocalizationKeys.Maps.QueryRequired)]
    public string Query { get; set; } = default!;

    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}
