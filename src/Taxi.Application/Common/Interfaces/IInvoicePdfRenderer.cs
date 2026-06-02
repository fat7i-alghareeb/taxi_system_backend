using Taxi.Domain.Invoices;

namespace Taxi.Application.Common.Interfaces;

/// <summary>
/// Renders a persisted <see cref="Invoice"/> snapshot into a PDF document.
/// </summary>
public interface IInvoicePdfRenderer
{
    /// <summary>
    /// Returns the PDF bytes for the given invoice, localized to the
    /// provided language code (falls back to English when missing).
    /// </summary>
    byte[] Render(Invoice invoice, string languageCode);
}
