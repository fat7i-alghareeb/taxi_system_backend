namespace Taxi.Domain.CustomerIncidents;

/// <summary>
/// The curated set of customer-facing problems and notable customer actions that
/// are worth surfacing to an admin. Deliberately NOT the full trip lifecycle —
/// normal events (driver assigned / en route / arrived / completed) are excluded
/// so the incident feed stays a "problems &amp; actions" list, not an event firehose.
/// </summary>
public enum CustomerIncidentType
{
    /// <summary>A payment (upfront fare or waiting fee) failed for the passenger.</summary>
    PaymentFailed = 0,

    /// <summary>The assigned driver cancelled the trip.</summary>
    TripCancelledByDriver = 1,

    /// <summary>The passenger cancelled the trip.</summary>
    TripCancelledByPassenger = 2,

    /// <summary>A refund was issued to the passenger.</summary>
    Refunded = 3,

    /// <summary>The passenger rated a completed trip 3 stars or fewer.</summary>
    LowRating = 4,

    /// <summary>The passenger filed a driver-late compensation claim.</summary>
    LateDriverCompensationClaim = 5,
}
