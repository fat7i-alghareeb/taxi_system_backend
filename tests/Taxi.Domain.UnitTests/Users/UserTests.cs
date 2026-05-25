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
}
