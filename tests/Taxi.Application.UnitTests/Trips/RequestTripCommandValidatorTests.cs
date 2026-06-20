using Taxi.Application.Features.Trips.Commands.RequestTrip;
using Taxi.Application.Features.Trips.Dtos;

using Xunit;

namespace Taxi.Application.UnitTests.Trips;

public class RequestTripCommandValidatorTests
{
    [Fact]
    public void Validate_WhenStopsLessThanTwo_ReturnsError()
    {
        var validator = new RequestTripCommandValidator();
        var command = new RequestTripCommand(
            Guid.NewGuid(),
            [new CoordinateDto(1, 2, "Label")]);

        var result = validator.Validate(command);

        Assert.Contains(result.Errors, e => e.PropertyName == "Stops");
    }

    [Fact]
    public void Validate_WhenValid_ReturnsNoErrors()
    {
        var validator = new RequestTripCommandValidator();
        var command = new RequestTripCommand(
            Guid.NewGuid(),
            [
                new CoordinateDto(1, 2, "Label 1"),
                new CoordinateDto(3, 4, "Label 2"),
            ]);

        var result = validator.Validate(command);

        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WhenAirportPickupHasNoFlightNumber_ReturnsError()
    {
        var validator = new RequestTripCommandValidator();
        var command = new RequestTripCommand(
            Guid.NewGuid(),
            [
                new CoordinateDto(1, 2, "Airport", IsAirport: true),
                new CoordinateDto(3, 4, "Destination"),
            ]);

        var result = validator.Validate(command);

        Assert.Contains(result.Errors, e => e.PropertyName == "FlightNumber");
    }

    [Theory]
    [InlineData("tk 1864")]
    [InlineData("U2 215")]
    [InlineData("3U8630")]
    public void Validate_WhenAirportFlightNumberIsValid_ReturnsNoErrors(
        string flightNumber)
    {
        var validator = new RequestTripCommandValidator();
        var command = new RequestTripCommand(
            Guid.NewGuid(),
            [
                new CoordinateDto(1, 2, "Airport", IsAirport: true),
                new CoordinateDto(3, 4, "Destination"),
            ],
            FlightNumber: flightNumber);

        var result = validator.Validate(command);

        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WhenOnlyDropoffIsAirport_DoesNotRequireFlightNumber()
    {
        var validator = new RequestTripCommandValidator();
        var command = new RequestTripCommand(
            Guid.NewGuid(),
            [
                new CoordinateDto(1, 2, "Pickup"),
                new CoordinateDto(3, 4, "Airport", IsAirport: true),
            ]);

        var result = validator.Validate(command);

        Assert.Empty(result.Errors);
    }
}
