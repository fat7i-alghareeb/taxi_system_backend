using Taxi.Contracts.Common;
using Taxi.Domain.Configuration;

using Xunit;

namespace Taxi.Domain.UnitTests.Configuration;

public class AppConfigTests
{
    [Fact]
    public void Create_WhenValid_ReturnsAppConfig()
    {
        var beforeCreate = DateTime.UtcNow;

        var result = AppConfig.Create("TripDiscountPercent", "5", "Global discount");

        Assert.True(result.IsSuccess);
        Assert.Equal("TripDiscountPercent", result.Value.Key);
        Assert.Equal("5", result.Value.Value);
        Assert.Equal("Global discount", result.Value.Description);
        Assert.True(result.Value.UpdatedAtUtc >= beforeCreate);
    }

    [Fact]
    public void Create_WhenKeyMissing_ReturnsKeyRequiredError()
    {
        var result = AppConfig.Create(string.Empty, "5");

        Assert.True(result.IsFailure);
        Assert.Equal(LocalizationKeys.AppConfig.KeyRequired, result.Error.Code);
    }

    [Fact]
    public void Create_WhenValueMissing_ReturnsValueRequiredError()
    {
        var result = AppConfig.Create("TripDiscountPercent", string.Empty);

        Assert.True(result.IsFailure);
        Assert.Equal(LocalizationKeys.AppConfig.ValueRequired, result.Error.Code);
    }

    [Fact]
    public void UpdateValue_WhenValid_UpdatesValueAndTimestamp()
    {
        var config = AppConfig.Create("TripDiscountPercent", "5").Value;
        var beforeUpdate = DateTime.UtcNow;

        var result = config.UpdateValue("10");

        Assert.True(result.IsSuccess);
        Assert.Equal("10", config.Value);
        Assert.True(config.UpdatedAtUtc >= beforeUpdate);
    }

    [Fact]
    public void UpdateValue_WhenValueMissing_ReturnsValueRequiredErrorAndKeepsExistingValue()
    {
        var config = AppConfig.Create("TripDiscountPercent", "5").Value;

        var result = config.UpdateValue(string.Empty);

        Assert.True(result.IsFailure);
        Assert.Equal(LocalizationKeys.AppConfig.ValueRequired, result.Error.Code);
        Assert.Equal("5", config.Value);
    }
}
