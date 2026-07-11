namespace Taxi.Contracts.Requests.Trips;

public class TripEditStopRequest
{
    public decimal Latitude { get; set; }

    public decimal Longitude { get; set; }

    public string? Label { get; set; }
}

/// <summary>Preview the fare difference for a proposed destination / passenger edit.</summary>
public class PreviewTripEditRequest
{
    /// <summary>New full stop list (pickup + destination …). Null keeps current stops.</summary>
    public List<TripEditStopRequest>? Stops { get; set; }

    /// <summary>New passenger count. Null keeps the current count.</summary>
    public int? PassengerCount { get; set; }
}

/// <summary>Apply a previewed edit and settle the fare difference.</summary>
public class ApplyTripEditRequest
{
    public List<TripEditStopRequest>? Stops { get; set; }

    public int? PassengerCount { get; set; }

    /// <summary>The delta the customer confirmed in the preview dialog (drift guard).</summary>
    public decimal ExpectedDelta { get; set; }
}
