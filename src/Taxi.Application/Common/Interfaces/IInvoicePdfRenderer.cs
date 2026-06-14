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
    /// <paramref name="contact"/> carries the live, admin-editable company
    /// contact details printed in the footer.
    /// </summary>
    byte[] Render(Invoice invoice, string languageCode, InvoiceContact contact);
}

/// <summary>Live company contact details printed on the invoice footer.</summary>
public sealed record InvoiceContact(string Email, string Phone, string Website);
