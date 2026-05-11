using Taxi.Domain.Common.Results;

namespace Taxi.Domain.Configuration;

public sealed class AppConfig
{
    private AppConfig() { }

    private AppConfig(string key, string value, string? description)
    {
        Key = key;
        Value = value;
        Description = description;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public string Key { get; private set; } = default!;
    public string Value { get; private set; } = default!;
    public string? Description { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static Result<AppConfig> Create(string key, string value, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return AppConfigErrors.KeyRequired;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            return AppConfigErrors.ValueRequired;
        }

        return new AppConfig(key, value, description);
    }

    public Result<Success> UpdateValue(string newValue)
    {
        if (string.IsNullOrWhiteSpace(newValue))
        {
            return AppConfigErrors.ValueRequired;
        }

        Value = newValue;
        UpdatedAtUtc = DateTime.UtcNow;

        return Result.Success;
    }
}

