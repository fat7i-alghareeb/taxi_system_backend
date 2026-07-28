using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Trips.Queries.GetTripInvoicePdf;

/// <summary>
/// Fetches the Fat7i-branded invoice PDF for a completed trip.
/// </summary>
/// <param name="TripId">The trip whose invoice should be rendered.</param>
/// <param name="LanguageCode">
/// Two-letter language code the customer chose for the PDF.
/// Pass <c>null</c> to use the default (Dutch). Unknown values fall back to
/// Dutch so the document is always usable. Supported codes:
/// <c>nl, en, ar, de, es, fr, pl, ro, uk</c>.
/// </param>
public record GetTripInvoicePdfQuery(Guid TripId, string? LanguageCode = null)
    : ICachedQuery<Result<TripInvoicePdfResult>>
{
    public string CacheKey => $"trip-invoice-pdf-{TripId}-{LanguageCode ?? "nl"}";

    public string[] Tags => [$"trip-financials-{TripId}"];

    public TimeSpan Expiration => TimeSpan.FromMinutes(30);
}

public sealed record TripInvoicePdfResult(string FileName, byte[] Bytes);
