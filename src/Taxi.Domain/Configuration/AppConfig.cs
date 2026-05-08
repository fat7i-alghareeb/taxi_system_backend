namespace Taxi.Domain.Configuration;

public sealed class AppConfig
{
    public AppConfig(string key, string value, string? description = null)
    {
        Key = key;
        Value = value;
        Description = description;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private AppConfig() { }

    public string Key { get; private set; } = default!;
    public string Value { get; private set; } = default!;
    public string? Description { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public void UpdateValue(string newValue)
    {
        Value = newValue;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

