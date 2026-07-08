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

    [MaxLength(500, ErrorMessage = LocalizationKeys.Trip.PassengerNoteTooLong)]
    public string? PassengerNote { get; set; }

    [MaxLength(15, ErrorMessage = LocalizationKeys.Trip.FlightNumberInvalid)]
    public string? FlightNumber { get; set; }

    /// <summary>"card" (default), "wallet", or "mixed". Absent/unknown is treated as "card".</summary>
    public string? PaymentMethod { get; set; }

    /// <summary>Optional saved reusable payment method to use for the card portion.</summary>
    public Guid? SavedPaymentMethodId { get; set; }
}

