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
            "not-a-phone",
            null,
            UserRole.Passenger);

        Assert.True(result.IsFailure);
        Assert.Equal(LocalizationKeys.User.PhoneRequired, result.Error.Code);
    }

    [Fact]
    public void UpdateProfile_WhenEmailProvided_TrimsAndStoresEmail()
    {
        var user = User.Create(
            Guid.NewGuid(),
            "John",
            "+31612345678",
            null,
            UserRole.Passenger).Value;

        var result = user.UpdateProfile("John", email: "  john@example.com  ");

        Assert.True(result.IsSuccess);
        Assert.Equal("john@example.com", user.Email);
    }

    [Fact]
    public void UpdateProfile_WhenEmailIsEmpty_ClearsEmail()
    {
        var user = User.Create(
            Guid.NewGuid(),
            "John",
            "+31612345678",
            "john@example.com",
            UserRole.Passenger).Value;

        var result = user.UpdateProfile("John", email: " ");

        Assert.True(result.IsSuccess);
        Assert.Null(user.Email);
    }

    [Fact]
    public void UpdateHomeAddress_WithFreeTextOnly_StoresAddress()
    {
        var user = User.Create(
            Guid.NewGuid(),
            "John",
            "+31612345678",
            null,
            UserRole.Passenger).Value;

        var result = user.UpdateHomeAddress("Main Street 10", null, null);

        Assert.True(result.IsSuccess);
        Assert.Equal("Main Street 10", user.HomeAddress?.Label);
        Assert.Null(user.HomeAddress?.Latitude);
        Assert.Null(user.HomeAddress?.Longitude);
    }

    [Fact]
    public void UpdateHomeAddress_WithOnlyOneCoordinate_IsRejected()
    {
        var user = User.Create(
            Guid.NewGuid(),
            "John",
            "+31612345678",
            null,
            UserRole.Passenger).Value;

        var result = user.UpdateHomeAddress("Main Street 10", 51.9m, null);

        Assert.True(result.IsFailure);
        Assert.Equal(LocalizationKeys.User.HomeAddressInvalid, result.Error.Code);
    }
}
