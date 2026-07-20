using Taxi.Domain.Common;

namespace Taxi.Domain.Trips.Events;

/// <summary>
/// Raised whenever a re-priced customer edit is actually committed onto a trip — from the
/// synchronous apply path and from the Stripe webhook that lands a held edit alike. This is the
/// passenger's only signal that the change went through and what it cost: without it the app
/// keeps rendering the pre-edit trip until something else forces a refetch, which is exactly how
/// a paid destination change used to look like nothing happened.
/// </summary>
public sealed class TripEditApplied : DomainEvent
{
    public Guid TripId { get; init; }

    public Guid PassengerId { get; init; }

    public Guid? DriverId { get; init; }

    public string ReferenceCode { get; init; } = string.Empty;

    /// <summary>The fare the trip now carries (the newly adopted quote).</summary>
    public decimal NewFare { get; init; }

    public string CurrencyCode { get; init; } = string.Empty;

    /// <summary>New fare minus old fare. Positive = charged, negative = refunded, 0 = no change.</summary>
    public decimal Delta { get; init; }

    public Guid VehicleTypeId { get; init; }

    public int PassengerCount { get; init; }

    public string? DropoffLabel { get; init; }

    public bool StopsChanged { get; init; }

    public bool PassengerCountChanged { get; init; }
}
