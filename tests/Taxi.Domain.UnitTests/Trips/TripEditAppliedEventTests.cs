using Taxi.Domain.Trips;
using Taxi.Domain.Trips.Events;
using Xunit;

namespace Taxi.Domain.UnitTests.Trips;

/// <summary>
/// A committed edit has to announce itself. The passenger app previously learned nothing when an
/// edit landed: the silent-settlement path returned a trip nobody re-rendered the fare from, and
/// the PaymentSheet path committed at the Stripe webhook, long after the HTTP response. Customers
/// saw an unchanged screen and concluded they had been changed without being charged.
/// </summary>
public class TripEditAppliedEventTests
{
    [Fact]
    public void RaiseEditApplied_CarriesTheSettledAmountAndCurrentDetails()
    {
        var trip = CreateAcceptedTrip();
        trip.ClearDomainEvents();

        trip.RaiseEditApplied(
            newFare: 32.50m,
            currencyCode: "eur",
            delta: 12.50m,
            stopsChanged: true,
            passengerCountChanged: false);

        var evt = Assert.IsType<TripEditApplied>(Assert.Single(trip.DomainEvents));
        Assert.Equal(trip.Id, evt.TripId);
        Assert.Equal(trip.PassengerId, evt.PassengerId);
        Assert.Equal(32.50m, evt.NewFare);
        Assert.Equal(12.50m, evt.Delta);
        Assert.Equal("eur", evt.CurrencyCode);
        Assert.True(evt.StopsChanged);
        Assert.False(evt.PassengerCountChanged);
        Assert.Equal(trip.DropoffStop!.AddressLabel, evt.DropoffLabel);
    }

    [Fact]
    public void RaiseEditApplied_AfterAVanUpgrade_ReportsTheNewVehicleAndParty()
    {
        var trip = CreateAcceptedTrip();
        var vanId = Guid.NewGuid();
        trip.UpdatePassengerCount(6, vanId);
        trip.UpdateQuote(Guid.NewGuid(), vanId);
        trip.ClearDomainEvents();

        trip.RaiseEditApplied(
            newFare: 41m,
            currencyCode: "eur",
            delta: 21m,
            stopsChanged: false,
            passengerCountChanged: true);

        var evt = Assert.IsType<TripEditApplied>(Assert.Single(trip.DomainEvents));
        Assert.Equal(vanId, evt.VehicleTypeId);
        Assert.Equal(6, evt.PassengerCount);
        Assert.True(evt.PassengerCountChanged);
    }

    [Fact]
    public void RaiseEditApplied_WithNoPriceChange_StillReportsZeroDelta()
    {
        // A same-price edit still moved the route; the app must refresh rather than assume.
        var trip = CreateAcceptedTrip();
        trip.ClearDomainEvents();

        trip.RaiseEditApplied(
            newFare: 15m,
            currencyCode: "eur",
            delta: 0m,
            stopsChanged: true,
            passengerCountChanged: false);

        var evt = Assert.IsType<TripEditApplied>(Assert.Single(trip.DomainEvents));
        Assert.Equal(0m, evt.Delta);
        Assert.Equal(15m, evt.NewFare);
    }

    private static Trip CreateAcceptedTrip()
    {
        var passengerId = Guid.NewGuid();
        var quote = PricingQuote.Create(
            Guid.NewGuid(), passengerId, Guid.NewGuid(),
            5m, 10m, 15m, 15m, 0m, "eur",
            DateTime.UtcNow.AddHours(1),
            [new Coordinate(52.37m, 4.89m), new Coordinate(52.38m, 4.90m)]).Value;

        var stops = new[]
        {
            TripStop.Create(new Coordinate(52.37m, 4.89m), 0, "From").Value,
            TripStop.Create(new Coordinate(52.38m, 4.90m), 1, "To").Value,
        };

        var trip = Trip.Request(
            Guid.NewGuid(), "TRP-EDITEV1", passengerId, quote, stops, scheduledAtUtc: null).Value;

        trip.CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-5);
        trip.ConfirmPayment();
        trip.AcceptByAdmin(Guid.NewGuid(), DateTimeOffset.UtcNow);
        return trip;
    }
}
