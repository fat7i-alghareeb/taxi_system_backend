using Taxi.Domain.Drivers;
using Taxi.Domain.Payments;
using Taxi.Domain.Trips;
using Taxi.Domain.Users;

namespace Taxi.Application.UnitTests.Infrastructure;

internal static class PaymentTestBuilders
{
    public static PricingQuote CreateValidQuote(Guid passengerId, Guid vehicleTypeId)
        => PricingQuote.Create(
            Guid.NewGuid(),
            passengerId,
            vehicleTypeId,
            totalDistanceKm: 5m,
            totalDurationMin: 10m,
            finalFare: 15.00m,
            originalFare: 15.00m,
            discountPercent: 0m,
            currencyCode: "eur",
            validUntil: DateTime.UtcNow.AddHours(1),
            stops: [new Coordinate(52.37m, 4.89m), new Coordinate(52.38m, 4.90m)]).Value;

    public static Trip CreateAwaitingPaymentTrip(Guid passengerId, Guid quoteId)
    {
        var vehicleTypeId = Guid.NewGuid();
        var quote = PricingQuote.Create(
            quoteId,
            passengerId,
            vehicleTypeId,
            5m,
            10m,
            15.00m,
            15.00m,
            0m,
            "eur",
            DateTime.UtcNow.AddHours(1),
            [new Coordinate(52.37m, 4.89m), new Coordinate(52.38m, 4.90m)]).Value;

        var stops = new[]
        {
            TripStop.Create(new Coordinate(52.37m, 4.89m), 0, "From").Value,
            TripStop.Create(new Coordinate(52.38m, 4.90m), 1, "To").Value,
        };

        return Trip.Request(
            Guid.NewGuid(),
            "TRP-TEST01",
            passengerId,
            quote,
            stops,
            scheduledAtUtc: null).Value;
    }

    public static Payment CreatePendingStripePayment(Guid tripId, string intentId = "pi_test_123")
        => Payment.CreateForStripe(
            Guid.NewGuid(),
            tripId,
            15.00m,
            "eur",
            intentId,
            "cs_test_secret").Value;

    public static Driver CreateActiveDriver()
        => Driver.Create(Guid.NewGuid(), Guid.NewGuid(), "DL-TEST-001").Value;

    public static User CreatePassenger(Guid id)
        => User.Create(
            id,
            "Test", "اختبار", "Test", "Test", "Test", "Test", "Test", "Test", "Test",
            "+31612345678",
            null,
            UserRole.Passenger).Value;
}
