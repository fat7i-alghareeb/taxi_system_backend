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
            Guid.NewGuid(),
            [new TripStopDto(1, 2, "Start")]);

        var result = validator.Validate(command);

        Assert.Contains(result.Errors, e => e.PropertyName == "Stops");
    }

    [Fact]
    public void Validate_WhenValid_ReturnsNoErrors()
    {
        var validator = new RequestTripCommandValidator();
        var command = new RequestTripCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [
                new TripStopDto(1, 2, "Start"),
                new TripStopDto(3, 4, "End"),
            ]);

        var result = validator.Validate(command);

        Assert.Empty(result.Errors);
    }
}
