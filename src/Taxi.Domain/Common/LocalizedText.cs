namespace Taxi.Domain.Common;

/// <summary>
/// A value object representing a bilingual string stored as a single JSONB column.
/// EF Core maps this via OwnsOne().ToJson() — properties must have setters for materialization.
/// </summary>
public sealed class LocalizedText
{
    public LocalizedText(string en, string ar, string nl)
    {
        this.En = en;
        this.Ar = ar;
        this.Nl = nl;
    }

    // Parameterless constructor required by EF Core for JSONB materialization
    private LocalizedText()
    {
        this.En = string.Empty;
        this.Ar = string.Empty;
        this.Nl = string.Empty;
    }

    public string En { get; private set; }

    public string Ar { get; private set; }

    public string Nl { get; private set; }

    public static LocalizedText Create(string en, string ar, string nl) => new(en, ar, nl);
}