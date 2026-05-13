namespace Taxi.Domain.Common;

/// <summary>
/// A value object representing a trilingual string stored as a single JSONB column.
/// EF Core maps this via OwnsOne().ToJson() — properties must have setters for materialization.
/// </summary>
public sealed class LocalizedText : ValueObject
{
    public LocalizedText(string en, string ar, string nl, string de, string pl, string uk, string fr, string es, string ro)
    {
        En = en;
        Ar = ar;
        Nl = nl;
        De = de;
        Pl = pl;
        Uk = uk;
        Fr = fr;
        Es = es;
        Ro = ro;
    }

    // Parameterless constructor required by EF Core for JSONB materialization
    private LocalizedText()
    {
        En = string.Empty;
        Ar = string.Empty;
        Nl = string.Empty;
        De = string.Empty;
        Pl = string.Empty;
        Uk = string.Empty;
        Fr = string.Empty;
        Es = string.Empty;
        Ro = string.Empty;
    }

    public string En { get; private set; }

    public string Ar { get; private set; }

    public string Nl { get; private set; }

    public string De { get; private set; }

    public string Pl { get; private set; }

    public string Uk { get; private set; }

    public string Fr { get; private set; }

    public string Es { get; private set; }

    public string Ro { get; private set; }

    public string GetTranslation(string languageCode)
    {
        return languageCode.ToLower() switch
        {
            "ar" => Ar,
            "nl" => Nl,
            "de" => De,
            "pl" => Pl,
            "uk" => Uk,
            "fr" => Fr,
            "es" => Es,
            "ro" => Ro,
            _ => En
        };
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return En;
        yield return Ar;
        yield return Nl;
        yield return De;
        yield return Pl;
        yield return Uk;
        yield return Fr;
        yield return Es;
        yield return Ro;
    }
}

