namespace Taxi.Domain.Invoices;

/// <summary>
/// Per-month counter row used to allocate the sequential portion of
/// <see cref="Invoice.InvoiceNumber"/>. One row per <c>YYYYMM</c> bucket.
/// </summary>
public sealed class InvoiceCounter
{
    public InvoiceCounter(string yearMonth, int nextSequence)
    {
        YearMonth = yearMonth;
        NextSequence = nextSequence;
    }

    private InvoiceCounter() { }

    /// <summary>Six-character bucket key, e.g. <c>"202606"</c>.</summary>
    public string YearMonth { get; private set; } = default!;

    public int NextSequence { get; private set; }

    public int Allocate()
    {
        var allocated = NextSequence;
        NextSequence++;
        return allocated;
    }
}
