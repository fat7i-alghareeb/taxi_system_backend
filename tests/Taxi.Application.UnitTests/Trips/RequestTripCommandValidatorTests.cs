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
}
