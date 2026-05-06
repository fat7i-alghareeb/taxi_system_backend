using Taxi.Contracts.Common;
using Taxi.Domain.Vehicles;

using Xunit;

namespace Taxi.Domain.UnitTests.Vehicles;

public class VehicleTypeTests
{
    [Fact]
    public void Create_WhenValid_ReturnsActiveVehicleType()
    {
        var result = VehicleType.Create(
            Guid.NewGuid(),
            "standard",
            "Standard",
            "Adi",
            "Standaard",
            4,
            2.8m,
            0.2m,
            5.0m);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsActive);
    }

    [Fact]
    public void Create_WhenCodeMissing_ReturnsCodeRequiredError()
    {
        var result = VehicleType.Create(
            Guid.NewGuid(),
            "",
            "Standard",
            "Adi",
            "Standaard",
            4,
            2.8m,
            0.2m,
            5.0m);

        Assert.True(result.IsFailure);
        Assert.Equal(LocalizationKeys.Vehicle.CodeRequired, result.Error.Code);
    }
}
