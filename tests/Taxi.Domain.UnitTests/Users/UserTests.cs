using Taxi.Contracts.Common;
using Taxi.Domain.Users;

using Xunit;

namespace Taxi.Domain.UnitTests.Users;

public class UserTests
{
    [Fact]
    public void Create_WhenPhoneInvalid_ReturnsPhoneRequiredError()
    {
        var result = User.Create(
            Guid.NewGuid(),
            "John",
            "Johan",
            "Jan",
            "John",
            "John",
            "John",
            "John",
            "John",
            "John",
            "not-a-phone",
            null,
            UserRole.Passenger);

        Assert.True(result.IsFailure);
        Assert.Equal(LocalizationKeys.User.PhoneRequired, result.Error.Code);
    }

    [Fact]
    public void AssignVehicle_WhenNotDriver_ReturnsValidationError()
    {
        var user = User.Create(
            Guid.NewGuid(),
            "John",
            "Johan",
            "Jan",
            "John",
            "John",
            "John",
            "John",
            "John",
            "John",
            "+1234567890",
            null,
            UserRole.Passenger).Value;

        var result = user.AssignVehicle(Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal("User.NotADriver", result.Error.Code);
    }
}
