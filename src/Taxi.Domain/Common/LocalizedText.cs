namespace Taxi.Domain.Common;

/// <summary>
/// A value object representing a trilingual string stored as a single JSONB column.
/// EF Core maps this via OwnsOne().ToJson() — properties must have setters for materialization.
/// </summary>
public sealed class LocalizedText : ValueObject
{
    public LocalizedText(string en, string ar, string nl)
    {
        En = en;
        Ar = ar;
        Nl = nl;
    }

    // Parameterless constructor required by EF Core for JSONB materialization
    private LocalizedText()
    {
        En = string.Empty;
        Ar = string.Empty;
        Nl = string.Empty;
    }

    public string En { get; private set; }

    public string Ar { get; private set; }

    public string Nl { get; private set; }

    public string GetTranslation(string languageCode)
    {
        return languageCode.ToLower() switch
        {
            "ar" => Ar,
            "nl" => Nl,
            _ => En
        };
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return En;
        yield return Ar;
        yield return Nl;
    }
}

